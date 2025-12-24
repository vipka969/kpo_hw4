using Microsoft.EntityFrameworkCore;
using OrderService.Models.Enums;

namespace OrderService.Models.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    [Precision(18, 2)]
    public decimal Amount { get; private set; }
    public string Description { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? UpdatedDate { get; private set; }

    private Order() { }

    public static Order Create(Guid userId, decimal amount, string description)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be empty", nameof(description));
        }

        return new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            Description = description,
            Status = OrderStatus.New,
            CreatedDate = DateTime.UtcNow
        };
    }

    public void MarkAsFinished()
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot finish a cancelled order");
        }

        Status = OrderStatus.Finished;
        UpdatedDate = DateTime.UtcNow;
    }

    public void MarkAsCancelled()
    {
        if (Status == OrderStatus.Finished)
        {
            throw new InvalidOperationException("Cannot cancel a finished order");
        }

        Status = OrderStatus.Cancelled;
        UpdatedDate = DateTime.UtcNow;
    }

    public bool CanBePaid()
    {
        return Status == OrderStatus.New;
    }
}