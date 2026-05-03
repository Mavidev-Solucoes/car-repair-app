using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.IntegrationTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class UserIntegrityTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public UserIntegrityTests(DatabaseFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync() => await _fixture.CleanDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Email_UniqueConstraint_PreventsDuplicateEmails()
    {
        await using var context = _fixture.CreateContext();

        var employee1 = new Employee("Alice", "alice@shop.com", "hash1", UserRole.Mechanic);
        var employee2 = new Employee("Bob", "alice@shop.com", "hash2", UserRole.Mechanic);

        await context.Users.AddAsync(employee1);
        await context.SaveChangesAsync();

        await context.Users.AddAsync(employee2);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(IsUniqueConstraintViolation(ex));
    }

    [Fact]
    public async Task Name_RequiredConstraint_PreventsSavingNullName()
    {
        await using var context = _fixture.CreateContext();

        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync(
                "INSERT INTO Users (Id, UserKind, Name, Email, PasswordHash, Role, IsActive, CreatedAt) " +
                "VALUES (NEWID(), 'Employee', NULL, 'noname@shop.com', 'hash', 2, 1, GETUTCDATE())"));

        Assert.Contains("Cannot insert the value NULL", ex.Message);
    }

    [Fact]
    public async Task Name_MaxLength_EnforcedByDatabase()
    {
        await using var context = _fixture.CreateContext();
        var longName = new string('A', 101);

        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO Users (Id, UserKind, Name, Email, PasswordHash, Role, IsActive, CreatedAt) " +
                $"VALUES (NEWID(), 'Employee', '{longName}', 'maxlen@shop.com', 'hash', 2, 1, GETUTCDATE())"));

        Assert.Contains("truncated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Email_RequiredConstraint_PreventsSavingNullEmail()
    {
        await using var context = _fixture.CreateContext();

        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync(
                "INSERT INTO Users (Id, UserKind, Name, Email, PasswordHash, Role, IsActive, CreatedAt) " +
                "VALUES (NEWID(), 'Employee', 'ValidName', NULL, 'hash', 2, 1, GETUTCDATE())"));

        Assert.Contains("Cannot insert the value NULL", ex.Message);
    }

    [Fact]
    public async Task Employee_CanBeSavedAndRetrieved()
    {
        await using var context = _fixture.CreateContext();

        var employee = new Employee("Charlie", "charlie@shop.com", "passwordhash", UserRole.Admin);
        await context.Users.AddAsync(employee);
        await context.SaveChangesAsync();

        var saved = await context.Users.FindAsync(employee.Id);
        Assert.NotNull(saved);
        Assert.Equal("Charlie", saved.Name);
        Assert.Equal("charlie@shop.com", saved.Email);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx && sqlEx.Number == 2601;
    }
}
