using CarRepairShop.Domain.Entities;
using CarRepairShop.Repository.Repositories;

namespace CarRepairShop.UnitTests.Repository;

public class ServiceOrderItemRepositoryTests
{
    private static async Task<(Customer customer, Employee employee, Vehicle vehicle, ServiceItem serviceItem)>
        SeedAsync(CarRepairShop.Repository.Context.CarRepairShopDbContext context)
    {
        var customer = DbContextFactory.MakeCustomer();
        var employee = DbContextFactory.MakeEmployee();
        context.Users.Add(customer);
        context.Users.Add(employee);
        await context.SaveChangesAsync();
        var vehicle = DbContextFactory.MakeVehicle(customer.Id);
        context.Vehicles.Add(vehicle);
        var item = DbContextFactory.MakeServiceItem();
        context.ServiceItems.Add(item);
        await context.SaveChangesAsync();
        return (customer, employee, vehicle, item);
    }

    [Fact]
    public async Task AddAsync_AndGetById_Works()
    {
        using var context = DbContextFactory.Create();
        var (customer, employee, vehicle, serviceItem) = await SeedAsync(context);
        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();

        var orderItem = new ServiceOrderItem(order.Id, serviceItem.Id, "Brake Pads", 50m, 2);
        context.ServiceOrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        var repo = new ServiceOrderItemRepository(context);
        var found = await repo.GetByIdAsync(orderItem.Id);

        Assert.NotNull(found);
        Assert.Equal(orderItem.Id, found!.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllItems()
    {
        using var context = DbContextFactory.Create();
        var (customer, employee, vehicle, serviceItem) = await SeedAsync(context);
        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();
        context.ServiceOrderItems.Add(new ServiceOrderItem(order.Id, serviceItem.Id, "Item A", 50m, 1));
        context.ServiceOrderItems.Add(new ServiceOrderItem(order.Id, serviceItem.Id, "Item B", 30m, 2));
        await context.SaveChangesAsync();

        var repo = new ServiceOrderItemRepository(context);
        var all = await repo.GetAllAsync();

        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task Delete_RemovesOrderItem()
    {
        using var context = DbContextFactory.Create();
        var (customer, employee, vehicle, serviceItem) = await SeedAsync(context);
        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();
        var orderItem = new ServiceOrderItem(order.Id, serviceItem.Id, "Brake Pads", 50m, 2);
        context.ServiceOrderItems.Add(orderItem);
        await context.SaveChangesAsync();

        var repo = new ServiceOrderItemRepository(context);
        repo.Delete(orderItem);
        await context.SaveChangesAsync();

        Assert.Equal(0, context.ServiceOrderItems.Count());
    }
}
