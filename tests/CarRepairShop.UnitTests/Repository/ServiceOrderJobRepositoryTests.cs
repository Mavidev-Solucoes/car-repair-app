using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Repository.Repositories;

namespace CarRepairShop.UnitTests.Repository;

public class ServiceOrderJobRepositoryTests
{
    private static async Task<(Customer customer, Employee employee, Vehicle vehicle, ServiceJob serviceJob, ServiceOrder order)>
        SeedAsync(CarRepairShop.Repository.Context.CarRepairShopDbContext context)
    {
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
        return (customer, employee, vehicle, catalogJob, order);
    }

    [Fact]
    public async Task GetByIdWithHistoryAsync_ExistingJob_ReturnsJobWithHistory()
    {
        using var context = DbContextFactory.Create();
        var (_, _, _, catalogJob, order) = await SeedAsync(context);
        var orderJob = new ServiceOrderJob(order.Id, catalogJob.Id, "Oil Change", "Desc", 50m);
        context.ServiceOrderJobs.Add(orderJob);
        await context.SaveChangesAsync();

        var repo = new ServiceOrderJobRepository(context);
        var found = await repo.GetByIdWithHistoryAsync(orderJob.Id);

        Assert.NotNull(found);
        Assert.Equal(orderJob.Id, found!.Id);
        Assert.NotEmpty(found.StatusHistory); // constructor creates initial status entry
    }

    [Fact]
    public async Task GetByIdWithHistoryAsync_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.Create();
        var repo = new ServiceOrderJobRepository(context);

        var found = await repo.GetByIdWithHistoryAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByIdWithServiceOrderAsync_ReturnsJobWithServiceOrder()
    {
        using var context = DbContextFactory.Create();
        var (_, _, _, catalogJob, order) = await SeedAsync(context);
        var orderJob = new ServiceOrderJob(order.Id, catalogJob.Id, "Oil Change", "Desc", 50m);
        context.ServiceOrderJobs.Add(orderJob);
        await context.SaveChangesAsync();

        var repo = new ServiceOrderJobRepository(context);
        var found = await repo.GetByIdWithServiceOrderAsync(orderJob.Id);

        Assert.NotNull(found);
        Assert.NotNull(found!.ServiceOrder);
    }

    [Fact]
    public async Task GetByIdWithServiceOrderAsync_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.Create();
        var repo = new ServiceOrderJobRepository(context);

        var found = await repo.GetByIdWithServiceOrderAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        using var context = DbContextFactory.Create();
        var (_, _, _, catalogJob, order) = await SeedAsync(context);
        context.ServiceOrderJobs.Add(new ServiceOrderJob(order.Id, catalogJob.Id, "Job A", "Desc", 50m));
        context.ServiceOrderJobs.Add(new ServiceOrderJob(order.Id, catalogJob.Id, "Job B", "Desc", 80m));
        context.ServiceOrderJobs.Add(new ServiceOrderJob(order.Id, catalogJob.Id, "Job C", "Desc", 30m));
        await context.SaveChangesAsync();

        var repo = new ServiceOrderJobRepository(context);
        var (items, totalCount) = await repo.GetPagedAsync(1, 2, "name", false);

        Assert.Equal(3, totalCount);
        Assert.Equal(2, items.Count());
    }

    [Theory]
    [InlineData("price", false)]
    [InlineData("price", true)]
    [InlineData("status", false)]
    [InlineData("status", true)]
    [InlineData("createdat", false)]
    [InlineData("createdat", true)]
    [InlineData("unknown", false)]
    public async Task GetPagedAsync_AllSortColumns_DoNotThrow(string orderBy, bool desc)
    {
        using var context = DbContextFactory.Create();
        var (_, _, _, catalogJob, order) = await SeedAsync(context);
        context.ServiceOrderJobs.Add(new ServiceOrderJob(order.Id, catalogJob.Id, "Job A", "Desc", 50m));
        await context.SaveChangesAsync();

        var repo = new ServiceOrderJobRepository(context);
        var (items, _) = await repo.GetPagedAsync(1, 10, orderBy, desc);

        Assert.Single(items);
    }

    [Fact]
    public async Task GetPagedAsync_WithFilter_ReturnsFiltered()
    {
        using var context = DbContextFactory.Create();
        var (_, _, _, catalogJob, order) = await SeedAsync(context);
        context.ServiceOrderJobs.Add(new ServiceOrderJob(order.Id, catalogJob.Id, "Job A", "Desc", 50m));
        context.ServiceOrderJobs.Add(new ServiceOrderJob(order.Id, catalogJob.Id, "Job B", "Desc", 80m));
        await context.SaveChangesAsync();

        var repo = new ServiceOrderJobRepository(context);
        var (items, totalCount) = await repo.GetPagedAsync(
            1, 10, "name", false,
            new System.Linq.Expressions.Expression<Func<ServiceOrderJob, bool>>[]
            {
                j => j.Name == "Job A"
            });

        Assert.Equal(1, totalCount);
        Assert.Equal("Job A", items.Single().Name);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsHistoryForJob()
    {
        using var context = DbContextFactory.Create();
        var (_, _, _, catalogJob, order) = await SeedAsync(context);
        var orderJob = new ServiceOrderJob(order.Id, catalogJob.Id, "Oil Change", "Desc", 50m);
        context.ServiceOrderJobs.Add(orderJob);
        await context.SaveChangesAsync();

        var repo = new ServiceOrderJobRepository(context);
        var history = await repo.GetHistoryAsync(orderJob.Id);

        // Constructor creates one initial history entry
        Assert.NotEmpty(history);
        Assert.Equal(orderJob.Id, history.First().ServiceOrderJobId);
    }

    [Fact]
    public async Task GetHistoryAsync_NonExistentJob_ReturnsEmpty()
    {
        using var context = DbContextFactory.Create();
        var repo = new ServiceOrderJobRepository(context);

        var history = await repo.GetHistoryAsync(Guid.NewGuid());

        Assert.Empty(history);
    }
}
