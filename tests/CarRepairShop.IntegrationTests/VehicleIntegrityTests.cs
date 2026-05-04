using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.IntegrationTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class VehicleIntegrityTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public VehicleIntegrityTests(DatabaseFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync() => await _fixture.CleanDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task LicensePlate_UniqueConstraint_PreventsDuplicates()
    {
        await using var context = _fixture.CreateContext();

        var customer = new Customer("Carlos", "55566677788", "carlos@example.com", "11955554444", "hash");
        await context.Users.AddAsync(customer);
        await context.SaveChangesAsync();

        var vehicle1 = new Vehicle(customer.Id, "Ford", "Focus", 2019, "XYZ5678");
        var vehicle2 = new Vehicle(customer.Id, "Honda", "Civic", 2021, "XYZ5678");

        await context.Vehicles.AddAsync(vehicle1);
        await context.SaveChangesAsync();

        await context.Vehicles.AddAsync(vehicle2);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(IsUniqueConstraintViolation(ex));
    }

    [Fact]
    public async Task LicensePlate_RequiredConstraint_PreventsSavingNull()
    {
        await using var context = _fixture.CreateContext();

        var customer = new Customer("Laura", "44455566677", "laura@example.com", "11944443333", "hash");
        await context.Users.AddAsync(customer);
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO Vehicles (Id, CustomerId, Brand, Model, Year, LicensePlate, CreatedAt) " +
                $"VALUES (NEWID(), '{customer.Id}', 'BMW', 'X5', 2022, NULL, GETUTCDATE())"));

        Assert.Contains("Cannot insert the value NULL", ex.Message);
    }

    [Fact]
    public async Task LicensePlate_MaxLength_EnforcedByDatabase()
    {
        await using var context = _fixture.CreateContext();

        var customer = new Customer("Fernanda", "33344455566", "fernanda@example.com", "11933332222", "hash");
        await context.Users.AddAsync(customer);
        await context.SaveChangesAsync();

        var tooLongPlate = new string('A', 8);
        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO Vehicles (Id, CustomerId, Brand, Model, Year, LicensePlate, CreatedAt) " +
                $"VALUES (NEWID(), '{customer.Id}', 'BMW', 'X5', 2022, '{tooLongPlate}', GETUTCDATE())"));

        Assert.Contains("truncated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Vehicle_ForeignKeyConstraint_RequiresValidCustomer()
    {
        await using var context = _fixture.CreateContext();
        var nonExistentCustomerId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO Vehicles (Id, CustomerId, Brand, Model, Year, LicensePlate, CreatedAt) " +
                $"VALUES (NEWID(), '{nonExistentCustomerId}', 'VW', 'Golf', 2023, 'DEF4321', GETUTCDATE())"));

        Assert.True(IsForeignKeyViolation(ex));
    }

    [Fact]
    public async Task Vehicle_DeletionRestricted_WhenServiceOrdersExist()
    {
        Guid vehicleId;

        await using (var context = _fixture.CreateContext())
        {
            var employee = new Employee("Mechanic", "mech@shop.com", "hash", UserRole.Mechanic);
            var customer = new Customer("Roberto", "22233344455", "roberto@example.com", "11922221111", "hash");
            await context.Users.AddRangeAsync(employee, customer);
            await context.SaveChangesAsync();

            var vehicle = new Vehicle(customer.Id, "Chevrolet", "Onix", 2021, "GHI7890");
            await context.Vehicles.AddAsync(vehicle);
            await context.SaveChangesAsync();

            var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
            await context.ServiceOrders.AddAsync(order);
            await context.SaveChangesAsync();

            vehicleId = vehicle.Id;
        }

        await using var deleteContext = _fixture.CreateContext();
        var vehicleToDelete = await deleteContext.Vehicles.SingleAsync(v => v.Id == vehicleId);
        deleteContext.Vehicles.Remove(vehicleToDelete);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());

        Assert.True(IsForeignKeyViolation(ex));
    }

    [Fact]
    public async Task Vehicle_CanBeSavedAndRetrieved()
    {
        await using var context = _fixture.CreateContext();

        var customer = new Customer("Lucia", "99988877766", "lucia@example.com", "11999998888", "hash");
        await context.Users.AddAsync(customer);
        await context.SaveChangesAsync();

        var vehicle = new Vehicle(customer.Id, "Renault", "Logan", 2018, "JKL0123", "White");
        await context.Vehicles.AddAsync(vehicle);
        await context.SaveChangesAsync();

        var saved = await context.Vehicles.FindAsync(vehicle.Id);
        Assert.NotNull(saved);
        Assert.Equal("Renault", saved.Brand);
        Assert.Equal("JKL0123", saved.LicensePlate);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx && sqlEx.Number == 2601;
    }

    private static bool IsForeignKeyViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx && sqlEx.Number == 547;
    }

    private static bool IsForeignKeyViolation(SqlException ex)
    {
        return ex.Number == 547;
    }
}
