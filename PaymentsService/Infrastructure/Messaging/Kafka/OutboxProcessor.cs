using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentsService.Application.Interfaces;
using PaymentsService.Infrastructure.Events;
using PaymentsService.Models.Entities;
using PaymentsService.Models.Enums;

namespace PaymentsService.Infrastructure.Messaging.Kafka;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IConfiguration _configuration;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger, IKafkaProducer kafkaProducer, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _kafkaProducer = kafkaProducer;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxProcessor");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();

        IOutboxRepository outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        IEnumerable<OutboxMessage> messages = await outboxRepository.GetPendingMessagesAsync(10);

        foreach (OutboxMessage message in messages)
        {
            await PublishMessageAsync(message, cancellationToken);

            message.Status = OutboxMesStatus.Processed;
            await outboxRepository.UpdateAsync(message);
        }
    }

    private async Task PublishMessageAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (message.EventType != nameof(PaymentProcessedEvent))
        {
            return;
        }

        try
        {
            PaymentProcessedEvent paymentProcessedEvent = 
                JsonSerializer.Deserialize<PaymentProcessedEvent>(message.Payload);
        
            if (paymentProcessedEvent == null)
            {
                _logger.LogError("Failed to deserialize PaymentProcessedEvent");
                return;
            }

            await _kafkaProducer.ProduceAsync(_configuration["Kafka:PaymentsTopic"], paymentProcessedEvent.EventId.ToString(), paymentProcessedEvent); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish PaymentProcessedEvent");
        }
    }
}
