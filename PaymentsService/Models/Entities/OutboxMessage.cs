using PaymentsService.Infrastructure.Events;
using PaymentsService.Models.Enums;

namespace PaymentsService.Models.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string EventType { get; private set; }
    public string Payload { get; private set; }
    public OutboxMesStatus Status { get; internal set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? ProcessedDate { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(Guid eventId, PaymentProcessedEvent @event)
    {
        string payload = System.Text.Json.JsonSerializer.Serialize(@event);

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = @event.GetType().Name,
            Payload = payload,
            Status = OutboxMesStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };
    }

    public void MarkAsProcessed()
    {
        Status = OutboxMesStatus.Processed;
        ProcessedDate = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = OutboxMesStatus.Failed;
        ProcessedDate = DateTime.UtcNow;
    }
}