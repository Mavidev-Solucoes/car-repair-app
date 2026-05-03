using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.IntegrationTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CarRepairShop.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ServiceOrderIntegrityTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public ServiceOrderIntegrityTests(DatabaseFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync() => await _fixture.CleanDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ServiceOrder_CanBeSavedAndRetrieved()
    {
        await using var context = _fixture.CreateContext();
        var (employee, customer, vehicle) = await SeedBasicEntitiesAsync(context);

        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        await context.ServiceOrders.AddAsync(order);
        await context.SaveChangesAsync();

        var saved = await context.ServiceOrders.FindAsync(order.Id);
        Assert.NotNull(saved);
        Assert.Equal(ServiceStatus.Received, saved.Status);
        Assert.Equal(vehicle.Id, saved.VehicleId);
        Assert.Equal(customer.Id, saved.CustomerId);
        Assert.Equal(employee.Id, saved.AssignedUserId);
    }

    [Fact]
    public async Task ServiceOrder_ForeignKeyConstraint_RequiresValidVehicle()
    {
        await using var context = _fixture.CreateContext();
        var (employee, customer, _) = await SeedBasicEntitiesAsync(context);
        var nonExistentVehicleId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO ServiceOrders (Id, VehicleId, CustomerId, AssignedUserId, Status, TotalPrice, CreatedAt) " +
                $"VALUES (NEWID(), '{nonExistentVehicleId}', '{customer.Id}', '{employee.Id}', 1, 0.00, GETUTCDATE())"));

        Assert.True(IsForeignKeyViolation(ex));
    }

    [Fact]
    public async Task ServiceOrder_ForeignKeyConstraint_RequiresValidCustomer()
    {
        await using var context = _fixture.CreateContext();
        var (employee, _, vehicle) = await SeedBasicEntitiesAsync(context);
        var nonExistentCustomerId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO ServiceOrders (Id, VehicleId, CustomerId, AssignedUserId, Status, TotalPrice, CreatedAt) " +
                $"VALUES (NEWID(), '{vehicle.Id}', '{nonExistentCustomerId}', '{employee.Id}', 1, 0.00, GETUTCDATE())"));

        Assert.True(IsForeignKeyViolation(ex));
    }

    [Fact]
    public async Task ServiceOrder_ForeignKeyConstraint_RequiresValidAssignedUser()
    {
        await using var context = _fixture.CreateContext();
        var (_, customer, vehicle) = await SeedBasicEntitiesAsync(context);
        var nonExistentUserId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync(
                $"INSERT INTO ServiceOrders (Id, VehicleId, CustomerId, AssignedUserId, Status, TotalPrice, CreatedAt) " +
                $"VALUES (NEWID(), '{vehicle.Id}', '{customer.Id}', '{nonExistentUserId}', 1, 0.00, GETUTCDATE())"));

        Assert.True(IsForeignKeyViolation(ex));
    }

    [Fact]
    public async Task ServiceOrder_Delete_CascadesToStatusHistory()
    {
        await using var context = _fixture.CreateContext();
        var (employee, customer, vehicle) = await SeedBasicEntitiesAsync(context);

        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        await context.ServiceOrders.AddAsync(order);
        await context.SaveChangesAsync();

        var orderId = order.Id;
        var historyCount = await context.ServiceStatusHistory
            .CountAsync(h => h.ServiceOrderId == orderId);
        Assert.True(historyCount > 0, "ServiceOrder should have initial status history.");

        context.ServiceOrders.Remove(order);
        await context.SaveChangesAsync();

        var remainingHistory = await context.ServiceStatusHistory
            .CountAsync(h => h.ServiceOrderId == orderId);
        Assert.Equal(0, remainingHistory);
    }

    [Fact]
    public async Task ServiceOrder_Delete_CascadesToServiceItems()
    {
        await using var context = _fixture.CreateContext();
        var (employee, customer, vehicle) = await SeedBasicEntitiesAsync(context);

        var catalogItem = new ServiceItem("Spark Plug", "Spark plug replacement", 25.00m, 50);
        await context.ServiceItems.AddAsync(catalogItem);
        await context.SaveChangesAsync();

        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        var orderItem = new ServiceOrderItem(order.Id, catalogItem.Id, "Spark plug", 25.00m, 2);
        order.AddServiceItem(orderItem, employee.Id);
        await context.ServiceOrders.AddAsync(order);
        await context.SaveChangesAsync();

        var orderId = order.Id;
        Assert.True(await context.ServiceOrderItems.AnyAsync(i => i.ServiceOrderId == orderId));

        context.ServiceOrders.Remove(order);
        await context.SaveChangesAsync();

        Assert.False(await context.ServiceOrderItems.AnyAsync(i => i.ServiceOrderId == orderId));
    }

    [Fact]
    public async Task ServiceOrder_DeletionRestricted_WhenVehicleIsDeleted()
    {
        await using var context = _fixture.CreateContext();
        var (employee, customer, vehicle) = await SeedBasicEntitiesAsync(context);

        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        await context.ServiceOrders.AddAsync(order);
        await context.SaveChangesAsync();

        context.Vehicles.Remove(vehicle);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(IsForeignKeyViolation(ex));
    }

    [Fact]
    public async Task ServiceOrder_StatusHistory_CreatedOnNewOrder()
    {
        await using var context = _fixture.CreateContext();
        var (employee, customer, vehicle) = await SeedBasicEntitiesAsync(context);

        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        await context.ServiceOrders.AddAsync(order);
        await context.SaveChangesAsync();

        var historyCount = await context.ServiceStatusHistory
            .CountAsync(h => h.ServiceOrderId == order.Id);
        Assert.Equal(1, historyCount);
    }

    private async Task<(Employee employee, Customer customer, Vehicle vehicle)> SeedBasicEntitiesAsync(
        Repository.Context.CarRepairShopDbContext context)
    {
        var employee = new Employee("Mechanic One", "mechanic1@shop.com", "hash", UserRole.Mechanic);
        var customer = new Customer("Customer One", "10020030040", "customer1@example.com", "11900001111", "hash");
        await context.Users.AddRangeAsync(employee, customer);
        await context.SaveChangesAsync();

        var vehicle = new Vehicle(customer.Id, "Toyota", "Camry", 2022, "MNO1122");
        await context.Vehicles.AddAsync(vehicle);
        await context.SaveChangesAsync();

        return (employee, customer, vehicle);
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
