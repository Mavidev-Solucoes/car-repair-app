using CarRepairShop.Domain.Entities;
using CarRepairShop.Repository.Repositories;

namespace CarRepairShop.UnitTests.Repository;

public class ServiceJobRepositoryTests
{
    [Fact]
    public async Task ExistsByNameAsync_Existing_ReturnsTrue()
    {
        using var context = DbContextFactory.Create();
        context.ServiceJobs.Add(DbContextFactory.MakeServiceJob("Oil Change"));
        await context.SaveChangesAsync();

        var repo = new ServiceJobRepository(context);
        var exists = await repo.ExistsByNameAsync("Oil Change");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_NotFound_ReturnsFalse()
    {
        using var context = DbContextFactory.Create();
        var repo = new ServiceJobRepository(context);

        var exists = await repo.ExistsByNameAsync("Nonexistent");

        Assert.False(exists);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        using var context = DbContextFactory.Create();
        context.ServiceJobs.Add(new ServiceJob("Oil Change", "Desc", 50m));
        context.ServiceJobs.Add(new ServiceJob("Brake Service", "Desc", 80m));
        context.ServiceJobs.Add(new ServiceJob("Tire Rotation", "Desc", 30m));
        await context.SaveChangesAsync();

        var repo = new ServiceJobRepository(context);
        var (items, totalCount) = await repo.GetPagedAsync(1, 2, "name", false);

        Assert.Equal(3, totalCount);
        Assert.Equal(2, items.Count());
    }

    [Theory]
    [InlineData("price", false)]
    [InlineData("price", true)]
    [InlineData("createdat", false)]
    [InlineData("createdat", true)]
    [InlineData("unknown", false)]
    public async Task GetPagedAsync_AllSortColumns_DoNotThrow(string orderBy, bool desc)
    {
        using var context = DbContextFactory.Create();
        context.ServiceJobs.Add(DbContextFactory.MakeServiceJob());
        await context.SaveChangesAsync();

        var repo = new ServiceJobRepository(context);
        var (items, _) = await repo.GetPagedAsync(1, 10, orderBy, desc);

        Assert.Single(items);
    }

    [Fact]
    public async Task GetPagedAsync_WithFilter_ReturnsFiltered()
    {
        using var context = DbContextFactory.Create();
        context.ServiceJobs.Add(new ServiceJob("Oil Change", "Desc", 50m));
        context.ServiceJobs.Add(new ServiceJob("Brake Service", "Desc", 80m));
        await context.SaveChangesAsync();

        var repo = new ServiceJobRepository(context);
        var (items, totalCount) = await repo.GetPagedAsync(
            1, 10, "name", false,
            new System.Linq.Expressions.Expression<Func<ServiceJob, bool>>[]
            {
                j => j.Name == "Oil Change"
            });

        Assert.Equal(1, totalCount);
        Assert.Equal("Oil Change", items.Single().Name);
    }

    [Fact]
    public async Task GetAverageTimesInProgressAsync_EmptyIds_ReturnsEmptyDictionary()
    {
        using var context = DbContextFactory.Create();
        var repo = new ServiceJobRepository(context);

        var result = await repo.GetAverageTimesInProgressAsync(Array.Empty<Guid>());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAverageTimesInProgressAsync_NoMatchingOrderJobs_ReturnsNullTimes()
    {
        using var context = DbContextFactory.Create();
        var job = DbContextFactory.MakeServiceJob();
        context.ServiceJobs.Add(job);
        await context.SaveChangesAsync();

        var repo = new ServiceJobRepository(context);
        var result = await repo.GetAverageTimesInProgressAsync(new[] { job.Id });

        Assert.Single(result);
        Assert.Null(result[job.Id]);
    }

    [Fact]
    public async Task GetAverageTimesInProgressAsync_WithNoHistory_ReturnsNullTimes()
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
        var serviceJob = DbContextFactory.MakeServiceJob();
        context.ServiceJobs.Add(serviceJob);
        await context.SaveChangesAsync();
        var order = new ServiceOrder(vehicle.Id, customer.Id, employee.Id);
        context.ServiceOrders.Add(order);
        await context.SaveChangesAsync();
        var orderJob = new ServiceOrderJob(
            order.Id, serviceJob.Id, "Oil Change", "Desc", 50m);
        context.ServiceOrderJobs.Add(orderJob);
        await context.SaveChangesAsync();

        var repo = new ServiceJobRepository(context);
        var result = await repo.GetAverageTimesInProgressAsync(new[] { serviceJob.Id });

        Assert.Single(result);
        // no InProgress -> Completed history -> null
        Assert.Null(result[serviceJob.Id]);
    }
}
