namespace OrderService.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken token = default);
    Task<bool> SaveEntitiesAsync(CancellationToken token = default);
}