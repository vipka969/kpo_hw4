namespace Shared;

public class OrderCreatedEvent
{
    public Guid EventId { get; set; }
    public DateTime OccurredOn { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;

    public OrderCreatedEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }

    public OrderCreatedEvent(Guid orderId, Guid userId, decimal amount, string description)
        : this()
    {
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
        Description = description;
    }
}