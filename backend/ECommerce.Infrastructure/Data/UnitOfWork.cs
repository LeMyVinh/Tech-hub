using ECommerce.Application;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync()
    {
        var transaction = await _context.Database.BeginTransactionAsync();
        return new EfUnitOfWorkTransaction(transaction);
    }

    private sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfUnitOfWorkTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync() => _transaction.CommitAsync();

        public Task RollbackAsync() => _transaction.RollbackAsync();

        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
