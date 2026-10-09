using ITS_Task.Business.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace ITS_Task.Middleware;
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            if (context.Response.HasStarted) throw;
            var (status, message) = exception switch
            {
                ApiException api => (api.StatusCode, api.Message),
                UnauthorizedAccessException => (401, "Authentication required"),
                ArgumentException argument => (400, argument.Message),
                DbUpdateConcurrencyException => (409, "Data changed concurrently; reload and retry"),
                DbUpdateException db when db.InnerException is SqlException sql && sql.Number is 2601 or 2627 => (409, "A record with these values already exists"),
                DbUpdateException db when db.InnerException is SqlException sql && sql.Number == 547 => (409, "The operation conflicts with related records"),
                RetryLimitExceededException retry when retry.InnerException is SqlException deadlock && deadlock.Number == 1205 => (409, "Concurrent operations conflicted; reload and retry"),
                SqlException sql when sql.Number == 1205 => (409, "A concurrent operation conflicted; retry"),
                _ => (500, "An unexpected server error occurred")
            };
            if (status >= 500) logger.LogError(exception, "Request failed: {TraceId}", context.TraceIdentifier);
            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new { statusCode = status, message });
        }
    }
}
