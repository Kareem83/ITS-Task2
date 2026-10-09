using ITS_Task;
using ITS_Task.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace ITS_Task.Tests;
public sealed class TestHost : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public TestHost() { connection.Open(); }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Initialize"] = "false", ["Seed:Enabled"] = "false", ["Seed:AdminPassword"] = "Admin@123", ["Seed:DemoPassword"] = "Demo@123",
            ["Jwt:Key"] = "Unit-Testing-Signing-Key-At-Least-32-Bytes-2026", ["Jwt:Issuer"] = "ITS.Tests", ["Jwt:Audience"] = "ITS.Tests", ["Jwt:DurationInMinutes"] = "10"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>(); services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        });
    }
    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<SeedInitializer>().InitializeAsync(true);
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}
