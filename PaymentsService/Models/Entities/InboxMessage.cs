using PaymentsService.Models.Enums;

namespace PaymentsService.Models.Entities;

public class InboxMessage
{
    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public string EventType { get; private set; }
    public string Payload { get; private set; }
    public InboxMesStatus Status { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? ProcessedDate { get; private set; }
    public string? Error { get; private set; }

    private InboxMessage() { }

    public static InboxMessage Create(Guid messageId, string eventType, object payload)
    {
        string payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

        return new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            EventType = eventType,
            Payload = payloadJson,
            Status = InboxMesStatus.New,
            CreatedDate = DateTime.UtcNow
        };
    }

    public void MarkAsProcessing()
    {
        if (Status != InboxMesStatus.New)
        {
            throw new InvalidOperationException("Only new messages can be marked as processing");
        }

        Status = InboxMesStatus.Processing;
    }

    public void MarkAsProcessed()
    {
        Status = InboxMesStatus.Processed;
        ProcessedDate = DateTime.UtcNow;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        Status = InboxMesStatus.Failed;
        ProcessedDate = DateTime.UtcNow;
        Error = error;
    }
}