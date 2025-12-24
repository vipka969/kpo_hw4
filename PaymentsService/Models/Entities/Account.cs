using Microsoft.EntityFrameworkCore;
using PaymentsService.Models.Enums;

namespace PaymentsService.Models.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    [Precision(18, 2)] 
    public decimal Balance { get; private set; }
    public int Version { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? UpdatedDate { get; private set; }

    private Account() { }

    public static Account Create(Guid userId, decimal initialBalance = 0)
    {
        if (initialBalance < 0)
            throw new ArgumentException("Initial balance cannot be negative", nameof(initialBalance));

        return new Account
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = initialBalance,
            Version = 0,
            Status = AccountStatus.Active,
            CreatedDate = DateTime.UtcNow
        };
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Deposit amount must be positive", nameof(amount));
        }

        if (Status != AccountStatus.Active)
        {
            throw new InvalidOperationException("Cannot deposit to inactive account");
        }

        Balance += amount;
        Version++;
        UpdatedDate = DateTime.UtcNow;
    }

    public bool TryWithdraw(decimal amount, out string errorMessage)
    {
        errorMessage = null;

        if (amount <= 0)
        {
            errorMessage = "Withdrawal amount must be positive";
            return false;
        }

        if (Status != AccountStatus.Active)
        {
            errorMessage = "Cannot withdraw from inactive account";
            return false;
        }

        if (Balance < amount)
        {
            errorMessage = "Insufficient funds";
            return false;
        }

        Balance -= amount;
        Version++;
        UpdatedDate = DateTime.UtcNow;
        return true;
    }

    public void Suspend()
    {
        Status = AccountStatus.Suspended;
        Version++;
        UpdatedDate = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        Status = AccountStatus.Active;
        Version++;
        UpdatedDate = DateTime.UtcNow;
    }
}