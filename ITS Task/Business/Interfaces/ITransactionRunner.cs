namespace ITS_Task.Business.Interfaces;
public interface ITransactionRunner
{
    Task<T> RunAsync<T>(Func<Task<T>> action);
}
