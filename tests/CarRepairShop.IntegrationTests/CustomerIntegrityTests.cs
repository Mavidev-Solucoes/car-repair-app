using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarRepairShop.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class CustomerIntegrityTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public CustomerIntegrityTests(DatabaseFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync() => await _fixture.CleanDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PersonalId_UniqueConstraint_PreventsDuplicates()
    {
        await using var context = _fixture.CreateContext();

        var customer1 = new Customer("Maria", "12345678901", "maria@example.com", "11999990000", "hash1");
        var customer2 = new Customer("Jose", "12345678901", "jose@example.com", "11888880000", "hash2");

        await context.Users.AddAsync(customer1);
        await context.SaveChangesAsync();

        await context.Users.AddAsync(customer2);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(IsUniqueConstraintViolation(ex));
    }

    [Fact]
    public async Task PersonalId_Column_AllowsNull_InUsersTableForTphMapping()
    {
        await using var context = _fixture.CreateContext();

        var affectedRows = await context.Database.ExecuteSqlRawAsync(
            $"INSERT INTO \"Users\" (\"Id\", \"UserKind\", \"Name\", \"Email\", \"PasswordHash\", \"Role\", \"IsActive\", \"PersonalId\", \"Telephone\", \"CreatedAt\") " +
            $"VALUES ('{Guid.NewGuid()}', 'Customer', 'Test', 'test@example.com', 'hash', 4, true, NULL, '11999990000', NOW())");

        Assert.Equal(1, affectedRows);
    }

    [Fact]
    public async Task Telephone_Column_AllowsNull_InUsersTableForTphMapping()
    {
        await using var context = _fixture.CreateContext();

        var affectedRows = await context.Database.ExecuteSqlRawAsync(
            $"INSERT INTO \"Users\" (\"Id\", \"UserKind\", \"Name\", \"Email\", \"PasswordHash\", \"Role\", \"IsActive\", \"PersonalId\", \"Telephone\", \"CreatedAt\") " +
            $"VALUES ('{Guid.NewGuid()}', 'Customer', 'Test', 'test2@example.com', 'hash', 4, true, '98765432100', NULL, NOW())");

        Assert.Equal(1, affectedRows);
    }

    [Fact]
    public async Task PersonalId_MaxLength_EnforcedByDatabase()
    {
        await using var context = _fixture.CreateContext();
        var tooLongId = new string('1', 15);

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO \"Users\" (\"Id\", \"UserKind\", \"Name\", \"Email\", \"PasswordHash\", \"Role\", \"IsActive\", \"PersonalId\", \"Telephone\", \"CreatedAt\") " +
                $"VALUES ('{Guid.NewGuid()}', 'Customer', 'Test', 'maxlen@example.com', 'hash', 4, true, '{tooLongId}', '11999990000', NOW())"));

        Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, ex.SqlState);
    }

    [Fact]
    public async Task Customer_CanBeSavedAndRetrieved()
    {
        await using var context = _fixture.CreateContext();

        var customer = new Customer("Ana Silva", "98765432100", "ana@example.com", "11988887777", "hash");
        await context.Users.AddAsync(customer);
        await context.SaveChangesAsync();

        var saved = await context.Users.OfType<Customer>().FirstOrDefaultAsync(c => c.Id == customer.Id);
        Assert.NotNull(saved);
        Assert.Equal("Ana Silva", saved.Name);
        Assert.Equal("98765432100", saved.PersonalId);
    }

    [Fact]
    public async Task Customer_DeletionRestricted_WhenVehiclesExist()
    {
        Guid customerId;

        await using (var context = _fixture.CreateContext())
        {
            var customer = new Customer("Pedro", "11122233344", "pedro@example.com", "11977776666", "hash");
            await context.Users.AddAsync(customer);
            await context.SaveChangesAsync();

            var vehicle = new Vehicle(customer.Id, "Toyota", "Corolla", 2020, "ABC1234");
            await context.Vehicles.AddAsync(vehicle);
            await context.SaveChangesAsync();

            customerId = customer.Id;
        }

        await using var deleteContext = _fixture.CreateContext();
        var customerToDelete = await deleteContext.Users.OfType<Customer>().SingleAsync(c => c.Id == customerId);
        deleteContext.Users.Remove(customerToDelete);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());

        Assert.True(IsForeignKeyViolation(ex));
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static bool IsForeignKeyViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.ForeignKeyViolation;
    }
}
