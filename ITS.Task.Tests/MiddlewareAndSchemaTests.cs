using System.Text.Json;
using ITS_Task.Business.Exceptions;
using ITS_Task.DataAccess;
using ITS_Task.Middleware;
using ITS_Task.Migrations;
using ITS_Task.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging.Abstractions;
namespace ITS_Task.Tests;
[Trait("Category", "Unit")]
public class MiddlewareAndSchemaTests
{
    [Fact] public async Task InternalErrorIs500WithoutSensitiveDetails()
    {
        var http = new DefaultHttpContext(); http.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new InvalidOperationException("secret connection details"), NullLogger<ExceptionHandlingMiddleware>.Instance);
        await middleware.InvokeAsync(http); Assert.Equal(500,http.Response.StatusCode);
        http.Response.Body.Position = 0; var text = await new StreamReader(http.Response.Body).ReadToEndAsync(); Assert.DoesNotContain("secret",text); Assert.Contains("unexpected",text);
    }
    [Theory] [InlineData(400)] [InlineData(403)] [InlineData(404)] [InlineData(409)]
    public async Task DomainErrorsPreserveMeaningfulStatus(int status)
    {
        var http = new DefaultHttpContext(); http.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new ApiException(status,"Domain error"),NullLogger<ExceptionHandlingMiddleware>.Instance);
        await middleware.InvokeAsync(http); Assert.Equal(status,http.Response.StatusCode);
    }
    [Fact] public void LegacyEmployeesCanHaveNoAccount()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=localhost;Database=MetadataOnly;Integrated Security=True").Options);
        Assert.True(context.Model.FindEntityType(typeof(Employee))!.FindProperty(nameof(Employee.UserId))!.IsNullable);
    }
    [Fact] public void RepairMigrationDoesNotDeleteHistoricalTablesOrRows()
    {
        var operations = new RepairEmployeeAccountRelationship().UpOperations;
        Assert.DoesNotContain(operations,o => o is DropTableOperation or DropColumnOperation or DeleteDataOperation);
        var index = Assert.Single(operations.OfType<CreateIndexOperation>(), i => i.Name == "IX_Employees_UserId"); Assert.True(index.IsUnique); Assert.Equal("[UserId] IS NOT NULL",index.Filter);
        Assert.Contains(operations.OfType<SqlOperation>(),o => o.Sql.Contains("THROW 51001"));
    }
}
