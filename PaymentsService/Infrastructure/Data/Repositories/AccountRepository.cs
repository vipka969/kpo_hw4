using Microsoft.EntityFrameworkCore;
using PaymentsService.Application.Interfaces;
using PaymentsService.Models.Entities;

namespace PaymentsService.Infrastructure.Data.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly PaymentsDbContext _context;

    public AccountRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<Account?> GetByIdAsync(Guid id)
    {
        return await _context.Accounts.FindAsync(id);
    }

    public async Task AddAsync(Account account)
    {
        await _context.Accounts.AddAsync(account);
    }

    public async Task UpdateAsync(Account account)
    {
        _context.Accounts.Update(account);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsForUserAsync(Guid userId)
    {
        return await _context.Accounts.AnyAsync(a => a.UserId == userId);
    }
}