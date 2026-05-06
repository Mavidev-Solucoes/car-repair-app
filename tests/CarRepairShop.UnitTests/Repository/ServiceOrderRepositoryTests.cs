using CarRepairShop.Domain.Entities;
using CarRepairShop.Repository.Repositories;

namespace CarRepairShop.UnitTests.Repository;

public class ServiceOrderRepositoryTests
{
    private static async Task<(Customer customer, Employee employee, Vehicle vehicle)>
        SeedUsersAndVehicleAsync(CarRepairShop.Repository.Context.CarRepairShopDbContext context)
    {
        var customer = DbContextFactory.MakeCustomer("Alice");
        var employee = DbContextFactory.MakeEmployee("Bob");
        context.Users.Add(customer);
        context.Users.Add(employee);
        await context.SaveChangesAsync();
        var vehicle = DbContextFactory.MakeVehicle(customer.Id);
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();
        return (customer, employee, vehicle);
    }

    [Fact]
    public async Task GetWithAllDetailsAsync_ExistingOrder_ReturnsOrder()
    {
        using var context = DbContextFactory.Create();
        var (customer, employee, vehicle) = await SeedUsersAndVehicleAsync(context);
        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();

        var repo = new ServiceOrderRepository(context);
        var found = await repo.GetWithAllDetailsAsync(order.Id);

        Assert.NotNull(found);
        Assert.Equal(order.Id, found!.Id);
    }

    [Fact]
    public async Task GetWithAllDetailsAsync_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.Create();
        var repo = new ServiceOrderRepository(context);

        var found = await repo.GetWithAllDetailsAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_ReturnsAllOrders()
    {
        using var context = DbContextFactory.Create();
        var (customer, employee, vehicle) = await SeedUsersAndVehicleAsync(context);
        context.ServiceOrders.Add(new ServiceOrder(vehicle.Id, customer.Id, employee.Id));
        context.ServiceOrders.Add(new ServiceOrder(vehicle.Id, customer.Id, employee.Id));
        await context.SaveChangesAsync();

        var repo = new ServiceOrderRepository(context);
        var all = await repo.GetAllWithDetailsAsync();

        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task GetStatusHistoryAsync_ReturnsHistoryForOrder()
    {
        using var context = DbContextFactory.Create();
        var (customer, employee, vehicle) = await SeedUsersAndVehicleAsync(context);
        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();

        var repo = new ServiceOrderRepository(context);
        var history = await repo.GetStatusHistoryAsync(order.Id);

        // ServiceOrder constructor creates initial status history entry
        Assert.NotEmpty(history);
    }

    [Fact]
    public async Task GetStatusHistoryAsync_NonExistentOrder_ReturnsEmpty()
    {
        using var context = DbContextFactory.Create();
        var repo = new ServiceOrderRepository(context);

        var history = await repo.GetStatusHistoryAsync(Guid.NewGuid());

        Assert.Empty(history);
    }
}
