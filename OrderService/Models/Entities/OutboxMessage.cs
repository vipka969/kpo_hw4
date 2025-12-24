using OrderService.Models.Enums;

namespace OrderService.Models.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public OutboxMesStatus Status { get; internal set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? ProcessedDate { get; internal set; }

    private OutboxMessage() { }

    public static OutboxMessage Create<TEvent>(Guid eventId, TEvent @event)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(@event);

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = typeof(TEvent).Name,
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