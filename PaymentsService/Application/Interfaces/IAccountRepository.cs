using PaymentsService.Models.Entities;

namespace PaymentsService.Application.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByUserIdAsync(Guid userId);
    Task<Account?> GetByIdAsync(Guid id);
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task<bool> ExistsForUserAsync(Guid userId);
}