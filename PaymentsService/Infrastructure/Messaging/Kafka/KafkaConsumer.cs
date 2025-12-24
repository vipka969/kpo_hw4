using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentsService.Application.Interfaces;
using PaymentsService.Infrastructure.Events;
using PaymentsService.Models.Entities;

namespace PaymentsService.Infrastructure.Messaging.Kafka;

public class KafkaConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly IConsumer<string, string> _consumer;
    private readonly string _topic;

    public KafkaConsumer(IServiceProvider serviceProvider, ILogger<KafkaConsumer> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        ConsumerConfig config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = "payments-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false // Для ручного коммита
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _topic = configuration["Kafka:OrdersTopic"] ?? "orders.created";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KafkaConsumer started for topic: {Topic}", _topic);
        _consumer.Subscribe(_topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);

                try
                {
                    await ProcessMessageAsync(result.Message.Key, result.Message.Value, stoppingToken);
                    _consumer.Commit(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message {Key}", result.Message.Key);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in KafkaConsumer");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _consumer.Close();
    }

    private async Task ProcessMessageAsync(string key, string json, CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();

        IInboxRepository inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Guid messageId = Guid.Parse(key);

        bool exists = await inboxRepository.ExistsByMessageIdAsync(messageId);
        if (exists)
        {
            _logger.LogInformation("Duplicate message detected: {MessageId}", messageId);
            return;
        }

        OrderCreatedEvent? orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

        InboxMessage inboxMessage = InboxMessage.Create(messageId, "OrderCreatedEvent", orderCreatedEvent);

        await inboxRepository.AddAsync(inboxMessage);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}