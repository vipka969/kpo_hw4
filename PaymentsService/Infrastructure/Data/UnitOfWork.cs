using PaymentsService.Application.Interfaces;

namespace PaymentsService.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly PaymentsDbContext _context;

    public UnitOfWork(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}