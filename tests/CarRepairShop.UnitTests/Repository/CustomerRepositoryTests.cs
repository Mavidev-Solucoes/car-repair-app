using CarRepairShop.Domain.Entities;
using CarRepairShop.Repository.Repositories;

namespace CarRepairShop.UnitTests.Repository;

public class CustomerRepositoryTests
{
    [Fact]
    public async Task GetByDocumentAsync_ExistingPersonalId_ReturnsCustomer()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer("Alice");
        context.Users.Add(customer);
        await context.SaveChangesAsync();

        var repo = new CustomerRepository(context);
        var found = await repo.GetByDocumentAsync("12345678901");

        Assert.NotNull(found);
        Assert.Equal("Alice", found!.Name);
    }

    [Fact]
    public async Task GetByDocumentAsync_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.Create();
        var repo = new CustomerRepository(context);

        var found = await repo.GetByDocumentAsync("00000000000");

        Assert.Null(found);
    }

    [Fact]
    public async Task ExistsByDocumentAsync_ExistingPersonalId_ReturnsTrue()
    {
        using var context = DbContextFactory.Create();
        context.Users.Add(DbContextFactory.MakeCustomer("Alice"));
        await context.SaveChangesAsync();

        var repo = new CustomerRepository(context);
        var exists = await repo.ExistsByDocumentAsync("12345678901");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByDocumentAsync_NotFound_ReturnsFalse()
    {
        using var context = DbContextFactory.Create();
        var repo = new CustomerRepository(context);

        var exists = await repo.ExistsByDocumentAsync("99999999999");

        Assert.False(exists);
    }

    [Fact]
    public async Task GetWithVehiclesAsync_ReturnsCustomerWithVehicles()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer("Alice");
        context.Users.Add(customer);
        await context.SaveChangesAsync();
        var vehicle = DbContextFactory.MakeVehicle(customer.Id);
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var repo = new CustomerRepository(context);
        var found = await repo.GetWithVehiclesAsync(customer.Id);

        Assert.NotNull(found);
        Assert.Single(found!.Vehicles);
    }

    [Fact]
    public async Task GetWithVehiclesAsync_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.Create();
        var repo = new CustomerRepository(context);

        var found = await repo.GetWithVehiclesAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task HasVehiclesAsync_WithVehicles_ReturnsTrue()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer("Alice");
        context.Users.Add(customer);
        await context.SaveChangesAsync();
        context.Vehicles.Add(DbContextFactory.MakeVehicle(customer.Id));
        await context.SaveChangesAsync();

        var repo = new CustomerRepository(context);
        var hasVehicles = await repo.HasVehiclesAsync(customer.Id);

        Assert.True(hasVehicles);
    }

    [Fact]
    public async Task HasVehiclesAsync_NoVehicles_ReturnsFalse()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer("Alice");
        context.Users.Add(customer);
        await context.SaveChangesAsync();

        var repo = new CustomerRepository(context);
        var hasVehicles = await repo.HasVehiclesAsync(customer.Id);

        Assert.False(hasVehicles);
    }

    [Fact]
    public async Task HasServiceOrdersAsync_WithOrders_ReturnsTrue()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer("Alice");
        context.Users.Add(customer);
        var employee = DbContextFactory.MakeEmployee("Bob");
        context.Users.Add(employee);
        await context.SaveChangesAsync();
        var vehicle = DbContextFactory.MakeVehicle(customer.Id);
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();
        context.ServiceOrders.Add(new ServiceOrder(vehicle.Id, customer.Id, employee.Id));
        await context.SaveChangesAsync();

        var repo = new CustomerRepository(context);
        var hasOrders = await repo.HasServiceOrdersAsync(customer.Id);

        Assert.True(hasOrders);
    }

    [Fact]
    public async Task HasServiceOrdersAsync_NoOrders_ReturnsFalse()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer("Alice");
        context.Users.Add(customer);
        await context.SaveChangesAsync();

        var repo = new CustomerRepository(context);
        var hasOrders = await repo.HasServiceOrdersAsync(customer.Id);

        Assert.False(hasOrders);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        using var context = DbContextFactory.Create();
        // PersonalId must be unique - create distinct ones
        context.Users.Add(new Customer("Alice", "11111111111", "alice@example.com", "11987654321", "hash"));
        context.Users.Add(new Customer("Bob", "22222222222", "bob@example.com", "11987654322", "hash"));
        context.Users.Add(new Customer("Carol", "33333333333", "carol@example.com", "11987654323", "hash"));
        await context.SaveChangesAsync();

        var repo = new CustomerRepository(context);
        var (items, totalCount) = await repo.GetPagedAsync(1, 2, "name", false);

        Assert.Equal(3, totalCount);
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task GetPagedAsync_WithFilter_ReturnsFilteredResults()
    {
        using var context = DbContextFactory.Create();
        context.Users.Add(new Customer("Alice", "11111111111", "alice@example.com", "11987654321", "hash"));
        context.Users.Add(new Customer("Bob", "22222222222", "bob@example.com", "11987654322", "hash"));
        await context.SaveChangesAsync();

        var repo = new CustomerRepository(context);
        var (items, totalCount) = await repo.GetPagedAsync(
            1, 10, "name", false,
            new System.Linq.Expressions.Expression<Func<Customer, bool>>[]
            {
                c => c.Name.Contains("Bob")
            });

        Assert.Equal(1, totalCount);
        Assert.Equal("Bob", items.Single().Name);
    }

    [Theory]
    [InlineData("email", false)]
    [InlineData("email", true)]
    [InlineData("createdat", false)]
    [InlineData("createdat", true)]
    [InlineData("personalid", false)]
    [InlineData("personalid", true)]
    [InlineData("unknown", false)]
    public async Task GetPagedAsync_AllSortColumns_DoNotThrow(string orderBy, bool desc)
    {
        using var context = DbContextFactory.Create();
        context.Users.Add(new Customer("Alice", "11111111111", "alice@example.com", "11987654321", "hash"));
        await context.SaveChangesAsync();

        var repo = new CustomerRepository(context);
        var (items, _) = await repo.GetPagedAsync(1, 10, orderBy, desc);

        Assert.Single(items);
    }
}
