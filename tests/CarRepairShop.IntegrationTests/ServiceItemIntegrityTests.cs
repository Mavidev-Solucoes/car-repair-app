using CarRepairShop.Domain.Entities;
using CarRepairShop.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarRepairShop.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ServiceItemIntegrityTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public ServiceItemIntegrityTests(DatabaseFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync() => await _fixture.CleanDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ServiceItem_CanBeSavedAndRetrieved()
    {
        await using var context = _fixture.CreateContext();

        var item = new ServiceItem("Oil Filter", "Standard oil filter replacement", 49.99m, 10);
        await context.ServiceItems.AddAsync(item);
        await context.SaveChangesAsync();

        var saved = await context.ServiceItems.FindAsync(item.Id);
        Assert.NotNull(saved);
        Assert.Equal("Oil Filter", saved.Name);
        Assert.Equal(10, saved.Stock);
        Assert.Equal(49.99m, saved.Price);
    }

    [Fact]
    public async Task ServiceItem_Name_RequiredConstraint_PreventsSavingNull()
    {
        await using var context = _fixture.CreateContext();

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO \"ServiceItems\" (\"Id\", \"Name\", \"Description\", \"Price\", \"Stock\", \"CreatedAt\") " +
                $"VALUES ('{Guid.NewGuid()}', NULL, 'Some description', 10.00, 5, NOW())"));

        Assert.Equal(PostgresErrorCodes.NotNullViolation, ex.SqlState);
    }

    [Fact]
    public async Task ServiceItem_Description_RequiredConstraint_PreventsSavingNull()
    {
        await using var context = _fixture.CreateContext();

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO \"ServiceItems\" (\"Id\", \"Name\", \"Description\", \"Price\", \"Stock\", \"CreatedAt\") " +
                $"VALUES ('{Guid.NewGuid()}', 'Some Item', NULL, 10.00, 5, NOW())"));

        Assert.Equal(PostgresErrorCodes.NotNullViolation, ex.SqlState);
    }

    [Fact]
    public async Task ServiceItem_Name_MaxLength_EnforcedByDatabase()
    {
        await using var context = _fixture.CreateContext();
        var longName = new string('X', 101);

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO \"ServiceItems\" (\"Id\", \"Name\", \"Description\", \"Price\", \"Stock\", \"CreatedAt\") " +
                $"VALUES ('{Guid.NewGuid()}', '{longName}', 'Description', 10.00, 5, NOW())"));

        Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, ex.SqlState);
    }

    [Fact]
    public async Task ServiceItem_Description_MaxLength_EnforcedByDatabase()
    {
        await using var context = _fixture.CreateContext();
        var longDescription = new string('D', 401);

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO \"ServiceItems\" (\"Id\", \"Name\", \"Description\", \"Price\", \"Stock\", \"CreatedAt\") " +
                $"VALUES ('{Guid.NewGuid()}', 'ValidName', '{longDescription}', 10.00, 5, NOW())"));

        Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, ex.SqlState);
    }

    [Fact]
    public async Task ServiceJob_CanBeSavedAndRetrieved()
    {
        await using var context = _fixture.CreateContext();

        var job = new ServiceJob("Brake Pad Replacement", "Front and rear brake pads", 299.99m);
        await context.ServiceJobs.AddAsync(job);
        await context.SaveChangesAsync();

        var saved = await context.ServiceJobs.FindAsync(job.Id);
        Assert.NotNull(saved);
        Assert.Equal("Brake Pad Replacement", saved.Name);
        Assert.Equal(299.99m, saved.Price);
    }

    [Fact]
    public async Task ServiceJob_Name_RequiredConstraint_PreventsSavingNull()
    {
        await using var context = _fixture.CreateContext();

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO \"ServiceJobs\" (\"Id\", \"Name\", \"Description\", \"Price\", \"CreatedAt\") " +
                $"VALUES ('{Guid.NewGuid()}', NULL, 'Some description', 10.00, NOW())"));

        Assert.Equal(PostgresErrorCodes.NotNullViolation, ex.SqlState);
    }

    [Fact]
    public async Task ServiceJob_Name_MaxLength_EnforcedByDatabase()
    {
        await using var context = _fixture.CreateContext();
        var longName = new string('J', 101);

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO \"ServiceJobs\" (\"Id\", \"Name\", \"Description\", \"Price\", \"CreatedAt\") " +
                $"VALUES ('{Guid.NewGuid()}', '{longName}', 'Description', 100.00, NOW())"));

        Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, ex.SqlState);
    }
}
