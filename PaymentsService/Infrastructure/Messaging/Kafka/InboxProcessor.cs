using PaymentsService.Application.Interfaces;
using PaymentsService.Application.UseCases.ProcessPayment;
using PaymentsService.Infrastructure.Events;
using PaymentsService.Models.Entities;

namespace PaymentsService.Infrastructure.Messaging.Kafka;

public class InboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InboxProcessor> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    public InboxProcessor(IServiceProvider serviceProvider, ILogger<InboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InboxProcessor started");
        
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNewMessagesAsync();
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("InboxProcessor stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InboxProcessor");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
        
        _logger.LogInformation("InboxProcessor stopped");
    }

    private async Task ProcessNewMessagesAsync()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        
        IInboxRepository inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        ProcessPaymentHandler processPaymentHandler = scope.ServiceProvider.GetRequiredService<ProcessPaymentHandler>();

        IEnumerable<InboxMessage> messages = await inboxRepository.GetNewMessagesAsync(100);

        foreach (InboxMessage message in messages)
        {
            try
            {
                message.MarkAsProcessing();
                await inboxRepository.UpdateAsync(message);
                await unitOfWork.SaveChangesAsync();

                OrderCreatedEvent? orderCreatedEvent = System.Text.Json.JsonSerializer.Deserialize<OrderCreatedEvent>(
                    message.Payload);

                if (orderCreatedEvent != null)
                {
                    PaymentProcessedEvent paymentEvent = await processPaymentHandler.Handle(
                        orderCreatedEvent.OrderId,
                        orderCreatedEvent.UserId,
                        orderCreatedEvent.Amount,
                        orderCreatedEvent.EventId);

                    if (paymentEvent.IsSuccess)
                    {
                        message.MarkAsProcessed();
                        _logger.LogInformation("Payment processed successfully for order {OrderId}", orderCreatedEvent.OrderId);
                    }
                    else
                    {
                        message.MarkAsFailed(paymentEvent.FailureReason ?? "Unknown error");
                        _logger.LogWarning("Payment failed for order {OrderId}: {Error}", orderCreatedEvent.OrderId, paymentEvent.FailureReason);
                    }
                }
                else
                {
                    message.MarkAsFailed("Failed to deserialize event");
                }

                await inboxRepository.UpdateAsync(message);
                await unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process inbox message {MessageId}", message.MessageId);
                message.MarkAsFailed(ex.Message);
                await inboxRepository.UpdateAsync(message);
                await unitOfWork.SaveChangesAsync();
            }
        }
    }
}