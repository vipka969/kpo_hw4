namespace PaymentsService.Infrastructure.Events;

public class PaymentProcessedEvent
{
    public Guid EventId { get; set; }
    public DateTime OccurredOn { get; set; }
    public Guid OrderId { get; set; }
    public Guid CorrelationId { get; set; }
    public bool IsSuccess { get; set; }
    public string FailureReason { get; set; }
    public decimal Amount { get; set; }

    public PaymentProcessedEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        FailureReason = string.Empty;
    }

    public static PaymentProcessedEvent Success(Guid orderId, Guid correlationId, decimal amount)
    {
        return new PaymentProcessedEvent
        {
            OrderId = orderId,
            CorrelationId = correlationId,
            IsSuccess = true,
            Amount = amount,
            FailureReason = string.Empty
        };
    }

    public static PaymentProcessedEvent Failed(Guid orderId, Guid correlationId, string reason, decimal amount)
    {
        return new PaymentProcessedEvent
        {
            OrderId = orderId,
            CorrelationId = correlationId,
            IsSuccess = false,
            Amount = amount,
            FailureReason = reason
        };
    }
}