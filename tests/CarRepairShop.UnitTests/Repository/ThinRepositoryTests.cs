using CarRepairShop.Domain.Entities;
using CarRepairShop.Repository.Repositories;

namespace CarRepairShop.UnitTests.Repository;

/// <summary>
/// Tests for thin repositories that only inherit from Repository{T} with no custom methods.
/// These tests ensure the constructors and base Repository operations are covered.
/// </summary>
public class ThinRepositoryTests
{
    [Fact]
    public async Task ServiceStatusHistoryRepository_AddAndGetById_Works()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer();
        var employee = DbContextFactory.MakeEmployee();
        context.Users.Add(customer);
        context.Users.Add(employee);
        await context.SaveChangesAsync();
        var vehicle = DbContextFactory.MakeVehicle(customer.Id);
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();
        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();

        // ServiceStatusHistory entries are created by the ServiceOrder constructor
        var repo = new ServiceStatusHistoryRepository(context);
        var all = await repo.GetAllAsync();

        Assert.NotEmpty(all);
    }

    [Fact]
    public async Task ServiceOrderJobStatusHistoryRepository_AddAndGetAll_Works()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer();
        var employee = DbContextFactory.MakeEmployee();
        context.Users.Add(customer);
        context.Users.Add(employee);
        await context.SaveChangesAsync();
        var vehicle = DbContextFactory.MakeVehicle(customer.Id);
        context.Vehicles.Add(vehicle);
        var catalogJob = DbContextFactory.MakeServiceJob();
        context.ServiceJobs.Add(catalogJob);
        await context.SaveChangesAsync();
        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();
        var orderJob = new ServiceOrderJob(order.Id, catalogJob.Id, "Oil Change", "Desc", 50m);
        context.ServiceOrderJobs.Add(orderJob);
        await context.SaveChangesAsync();

        var repo = new ServiceOrderJobStatusHistoryRepository(context);
        var all = await repo.GetAllAsync();

        Assert.NotEmpty(all);
    }

    [Fact]
    public async Task ServiceStatusHistoryRepository_GetById_Works()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer();
        var employee = DbContextFactory.MakeEmployee();
        context.Users.Add(customer);
        context.Users.Add(employee);
        await context.SaveChangesAsync();
        var vehicle = DbContextFactory.MakeVehicle(customer.Id);
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();
        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();

        var history = context.ServiceStatusHistory.First();
        var repo = new ServiceStatusHistoryRepository(context);
        var found = await repo.GetByIdAsync(history.Id);

        Assert.NotNull(found);
    }

    [Fact]
    public async Task ServiceOrderJobStatusHistoryRepository_GetById_Works()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer();
        var employee = DbContextFactory.MakeEmployee();
        context.Users.Add(customer);
        context.Users.Add(employee);
        await context.SaveChangesAsync();
        var vehicle = DbContextFactory.MakeVehicle(customer.Id);
        context.Vehicles.Add(vehicle);
        var catalogJob = DbContextFactory.MakeServiceJob();
        context.ServiceJobs.Add(catalogJob);
        await context.SaveChangesAsync();
        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();
        var orderJob = new ServiceOrderJob(order.Id, catalogJob.Id, "Oil Change", "Desc", 50m);
        context.ServiceOrderJobs.Add(orderJob);
        await context.SaveChangesAsync();

        var history = context.ServiceOrderJobStatusHistory.First();
        var repo = new ServiceOrderJobStatusHistoryRepository(context);
        var found = await repo.GetByIdAsync(history.Id);

        Assert.NotNull(found);
    }
}
