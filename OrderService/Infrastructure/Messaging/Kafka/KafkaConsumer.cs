using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderService.Models.Entities;
using System.Text.Json;
using Confluent.Kafka;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.Events;
using IUnitOfWork = OrderService.Application.Interfaces.IUnitOfWork;

namespace OrderService.Infrastructure.Messaging.Kafka;

public class KafkaConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly IConfiguration _configuration;

    public KafkaConsumer(IServiceProvider serviceProvider, ILogger<KafkaConsumer> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConsumerConfig config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = "orders-payment-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_configuration["Kafka:PaymentsTopic"]);

        _logger.LogInformation("PaymentResultConsumer started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ConsumeResult<string, string>? result = consumer.Consume(stoppingToken);

                try
                {
                    await ProcessMessageAsync(result.Message.Key, result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process payment event {Key}", result.Message.Key);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PaymentResultConsumer");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(string key, string json, CancellationToken ct)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IOrderRepository orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            PaymentProcessedEvent? evt = JsonSerializer.Deserialize<PaymentProcessedEvent>(json, options);

            if (evt == null) 
            {
                _logger.LogWarning("Failed to deserialize PaymentProcessedEvent");
                return;
            }

            Order? order = await orders.GetByIdAsync(evt.OrderId);
            if (order == null) 
            {
                _logger.LogWarning("Order not found: {OrderId}", evt.OrderId);
                return;
            }

            if (!order.CanBePaid()) return;

            if (evt.IsSuccess)
            {
                order.MarkAsFinished();
            }
            else
            {
                order.MarkAsCancelled();
            }

            await orders.UpdateAsync(order);
            await unitOfWork.SaveChangesAsync(ct);
        
            _logger.LogInformation("Order {OrderId} status updated to {Status}", order.Id, order.Status);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error: {Json}", json);
        }
    }
}
