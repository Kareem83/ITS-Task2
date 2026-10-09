using System.Data;
using ITS_Task.Business.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace ITS_Task.DataAccess;
public sealed class TransactionRunner(AppDbContext context) : ITransactionRunner
{
    public async Task<T> RunAsync<T>(Func<Task<T>> action)
    {
        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var result = await action();
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                context.ChangeTracker.Clear();
                throw;
            }
        });
    }
}
