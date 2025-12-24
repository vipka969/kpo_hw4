using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.Events;
using OrderService.Models.Entities;
using OrderService.Models.Enums;

namespace OrderService.Infrastructure.Messaging.Kafka;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly KafkaProducer _kafkaProducer;
    private readonly IConfiguration _configuration;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger, KafkaProducer kafkaProducer, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _kafkaProducer = kafkaProducer;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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

        IOutboxRepository outboxRepository =
            scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        IEnumerable<OutboxMessage> messages =
            await outboxRepository.GetPendingMessagesAsync(10);

        foreach (OutboxMessage message in messages)
        {
            await PublishMessageAsync(message);

            message.Status = OutboxMesStatus.Processed;
            message.ProcessedDate = DateTime.UtcNow;

            await outboxRepository.UpdateAsync(message);
        }
    }

    private async Task PublishMessageAsync(OutboxMessage message)
    {
        if (message.EventType != nameof(OrderCreatedEvent))
        {
            return;
        }

        await _kafkaProducer.ProduceAsync(
            _configuration["Kafka:OrdersTopic"],
            message.EventId.ToString(),
            message.Payload);
    }
}
