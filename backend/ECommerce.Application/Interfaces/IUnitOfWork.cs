namespace ECommerce.Application;

public interface IUnitOfWork
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync();
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
