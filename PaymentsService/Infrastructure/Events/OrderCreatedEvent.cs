namespace PaymentsService.Infrastructure.Events;

public class OrderCreatedEvent
{
    public Guid EventId { get; set; }
    public DateTime OccurredOn { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }

    public OrderCreatedEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        Description = string.Empty;
    }

    public OrderCreatedEvent(Guid orderId, Guid userId, decimal amount, string description)
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
        Description = description;
    }
}