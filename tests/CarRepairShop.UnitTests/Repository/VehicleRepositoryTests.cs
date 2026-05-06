using CarRepairShop.Domain.Entities;
using CarRepairShop.Repository.Repositories;

namespace CarRepairShop.UnitTests.Repository;

public class VehicleRepositoryTests
{
    [Fact]
    public async Task GetByLicensePlateAsync_Existing_ReturnsVehicle()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer();
        context.Users.Add(customer);
        await context.SaveChangesAsync();
        var vehicle = DbContextFactory.MakeVehicle(customer.Id, "XYZ9876");
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);
        var found = await repo.GetByLicensePlateAsync("XYZ9876");

        Assert.NotNull(found);
        Assert.Equal("XYZ9876", found!.LicensePlate);
    }

    [Fact]
    public async Task GetByLicensePlateAsync_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.Create();
        var repo = new VehicleRepository(context);

        var found = await repo.GetByLicensePlateAsync("NOTHERE");

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ReturnsVehiclesForCustomer()
    {
        using var context = DbContextFactory.Create();
        var customer1 = DbContextFactory.MakeCustomer("Alice");
        var customer2 = DbContextFactory.MakeCustomer("Bob");
        // Use distinct personal IDs
        context.Users.Add(new Customer("Alice", "11111111111", "alice@example.com", "11987654321", "hash"));
        context.Users.Add(new Customer("Bob", "22222222222", "bob@example.com", "11987654322", "hash"));
        await context.SaveChangesAsync();
        var customers = context.Users.OfType<Customer>().ToList();
        var cust1 = customers[0];
        var cust2 = customers[1];
        context.Vehicles.Add(DbContextFactory.MakeVehicle(cust1.Id, "AAA1111"));
        context.Vehicles.Add(DbContextFactory.MakeVehicle(cust1.Id, "BBB2222"));
        context.Vehicles.Add(DbContextFactory.MakeVehicle(cust2.Id, "CCC3333"));
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);
        var vehicles = await repo.GetByCustomerIdAsync(cust1.Id);

        Assert.Equal(2, vehicles.Count());
    }

    [Fact]
    public async Task ExistsByLicensePlateAsync_Existing_ReturnsTrue()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer();
        context.Users.Add(customer);
        await context.SaveChangesAsync();
        context.Vehicles.Add(DbContextFactory.MakeVehicle(customer.Id, "DEF4567"));
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);
        var exists = await repo.ExistsByLicensePlateAsync("DEF4567");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByLicensePlateAsync_NotFound_ReturnsFalse()
    {
        using var context = DbContextFactory.Create();
        var repo = new VehicleRepository(context);

        var exists = await repo.ExistsByLicensePlateAsync("NOTHERE");

        Assert.False(exists);
    }

    [Fact]
    public async Task HasServiceOrdersAsync_WithOrders_ReturnsTrue()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer();
        var employee = DbContextFactory.MakeEmployee();
        context.Users.Add(customer);
        context.Users.Add(employee);
        await context.SaveChangesAsync();
        var vehicle = DbContextFactory.MakeVehicle(customer.Id, "GHI7890");
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();
        context.ServiceOrders.Add(new ServiceOrder(vehicle.Id, customer.Id, employee.Id));
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);
        var hasOrders = await repo.HasServiceOrdersAsync(vehicle.Id);

        Assert.True(hasOrders);
    }

    [Fact]
    public async Task HasServiceOrdersAsync_NoOrders_ReturnsFalse()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer();
        context.Users.Add(customer);
        await context.SaveChangesAsync();
        var vehicle = DbContextFactory.MakeVehicle(customer.Id, "JKL1234");
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);
        var hasOrders = await repo.HasServiceOrdersAsync(vehicle.Id);

        Assert.False(hasOrders);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer();
        context.Users.Add(customer);
        await context.SaveChangesAsync();
        context.Vehicles.Add(new Vehicle(customer.Id, "Ford", "Focus", 2022, "AAA1111"));
        context.Vehicles.Add(new Vehicle(customer.Id, "Honda", "Civic", 2021, "BBB2222"));
        context.Vehicles.Add(new Vehicle(customer.Id, "Toyota", "Camry", 2020, "CCC3333"));
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);
        var (items, totalCount) = await repo.GetPagedAsync(1, 2, "brand", false);

        Assert.Equal(3, totalCount);
        Assert.Equal(2, items.Count());
    }

    [Theory]
    [InlineData("model", false)]
    [InlineData("model", true)]
    [InlineData("year", false)]
    [InlineData("year", true)]
    [InlineData("licenseplate", false)]
    [InlineData("licenseplate", true)]
    [InlineData("createdat", false)]
    [InlineData("createdat", true)]
    [InlineData("unknown", false)]
    public async Task GetPagedAsync_AllSortColumns_DoNotThrow(string orderBy, bool desc)
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer();
        context.Users.Add(customer);
        await context.SaveChangesAsync();
        context.Vehicles.Add(new Vehicle(customer.Id, "Ford", "Focus", 2022, "MNO5678"));
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);
        var (items, _) = await repo.GetPagedAsync(1, 10, orderBy, desc);

        Assert.Single(items);
    }

    [Fact]
    public async Task GetPagedAsync_WithFilter_ReturnsFilteredResults()
    {
        using var context = DbContextFactory.Create();
        var customer = DbContextFactory.MakeCustomer();
        context.Users.Add(customer);
        await context.SaveChangesAsync();
        context.Vehicles.Add(new Vehicle(customer.Id, "Ford", "Focus", 2022, "AAA1111"));
        context.Vehicles.Add(new Vehicle(customer.Id, "Honda", "Civic", 2021, "BBB2222"));
        await context.SaveChangesAsync();

        var repo = new VehicleRepository(context);
        var (items, totalCount) = await repo.GetPagedAsync(
            1, 10, "brand", false,
            new System.Linq.Expressions.Expression<Func<Vehicle, bool>>[]
            {
                v => v.Brand == "Ford"
            });

        Assert.Equal(1, totalCount);
        Assert.Equal("Ford", items.Single().Brand);
    }
}
