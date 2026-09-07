using CarRepairShop.Repository.Context;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CarRepairShop.IntegrationTests.Infrastructure;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString { get; private set; } = default!;

    public DatabaseFixture()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public CarRepairShopDbContext CreateContext()
    {
        return new CarRepairShopDbContext(BuildOptions(ConnectionString));
    }

    /// <summary>
    /// Deletes all rows from every table in FK-safe order so each test class
    /// starts with a clean database.
    /// </summary>
    public async Task CleanDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                "ServiceOrderJobStatusHistory",
                "ServiceOrderJobs",
                "ServiceOrderItems",
                "ServiceStatusHistory",
                "ServiceOrders",
                "Vehicles",
                "Users",
                "ServiceItems",
                "ServiceJobs"
            RESTART IDENTITY CASCADE;
            """);
    }

    private static DbContextOptions<CarRepairShopDbContext> BuildOptions(string connectionString)
    {
        return new DbContextOptionsBuilder<CarRepairShopDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }
}
