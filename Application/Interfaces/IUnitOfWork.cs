namespace OrderApi.Infrastructure.Persistence;

public interface IUnitOfWork : IDisposable
{
    Task<bool> CommitAsync();
}
