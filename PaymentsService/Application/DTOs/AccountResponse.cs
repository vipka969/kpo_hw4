using PaymentsService.Models.Entities;

namespace PaymentsService.Application.DTOs;

public sealed record AccountResponse(
    Guid Id,
    Guid UserId,
    decimal Balance,
    string Status,
    DateTime CreatedAt)
{
    public static AccountResponse FromDomain(Account account) =>
        new(
            account.Id,
            account.UserId,
            account.Balance,
            account.Status.ToString(),
            account.CreatedDate);
}