using CarRepairShop.Domain.Entities;

namespace CarRepairShop.UnitTests.Entities;

public class VehicleTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private const string Brand = "Toyota";
    private const string Model = "Corolla";
    private const int Year = 2020;
    private const string LicensePlate = "ABC1D23";

    // --- Constructor ---

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate, "Red");

        Assert.Equal(CustomerId, vehicle.CustomerId);
        Assert.Equal(Brand, vehicle.Brand);
        Assert.Equal(Model, vehicle.Model);
        Assert.Equal(Year, vehicle.Year);
        Assert.Equal(LicensePlate, vehicle.LicensePlate);
        Assert.Equal("Red", vehicle.Color);
    }

    [Fact]
    public void Constructor_WithNullColor_SetsColorToNull()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate);

        Assert.Null(vehicle.Color);
    }

    [Fact]
    public void Constructor_StripsDashesFromLicensePlate()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, "ABC-1D23");

        Assert.Equal("ABC1D23", vehicle.LicensePlate);
    }

    [Fact]
    public void Constructor_UpperCasesLicensePlate()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, "abc1d23");

        Assert.Equal("ABC1D23", vehicle.LicensePlate);
    }

    [Fact]
    public void Constructor_StripsDashesAndUpperCasesLicensePlate()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, "abc-1d23");

        Assert.Equal("ABC1D23", vehicle.LicensePlate);
    }

    [Fact]
    public void Constructor_WithCreatedUserId_SetsCreatedUserId()
    {
        var userId = Guid.NewGuid();
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate, createdUserId: userId);

        Assert.Equal(userId, vehicle.CreatedUserId);
    }

    [Fact]
    public void Constructor_WithoutCreatedUserId_LeavesCreatedUserIdNull()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate);

        Assert.Null(vehicle.CreatedUserId);
    }

    [Fact]
    public void Constructor_AssignsNewId()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate);

        Assert.NotEqual(Guid.Empty, vehicle.Id);
    }

    [Fact]
    public void Constructor_SetsCreatedAt()
    {
        var before = DateTime.UtcNow;
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate);
        var after = DateTime.UtcNow;

        Assert.InRange(vehicle.CreatedAt, before, after);
    }

    [Fact]
    public void Constructor_ServiceOrdersCollectionIsEmpty()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate);

        Assert.Empty(vehicle.ServiceOrders);
    }

    // --- Update ---

    [Fact]
    public void Update_ChangesAllProperties()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate, "Red");

        vehicle.Update("Honda", "Civic", 2022, "XYZ9W87", "Blue");

        Assert.Equal("Honda", vehicle.Brand);
        Assert.Equal("Civic", vehicle.Model);
        Assert.Equal(2022, vehicle.Year);
        Assert.Equal("XYZ9W87", vehicle.LicensePlate);
        Assert.Equal("Blue", vehicle.Color);
    }

    [Fact]
    public void Update_StripsDashesFromLicensePlate()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate);

        vehicle.Update(Brand, Model, Year, "XYZ-9W87", null);

        Assert.Equal("XYZ9W87", vehicle.LicensePlate);
    }

    [Fact]
    public void Update_UpperCasesLicensePlate()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate);

        vehicle.Update(Brand, Model, Year, "xyz9w87", null);

        Assert.Equal("XYZ9W87", vehicle.LicensePlate);
    }

    [Fact]
    public void Update_SetsColorToNull_WhenNullProvided()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate, "Red");

        vehicle.Update(Brand, Model, Year, LicensePlate, null);

        Assert.Null(vehicle.Color);
    }

    [Fact]
    public void Update_SetsUpdatedAt()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate);
        var before = DateTime.UtcNow;

        vehicle.Update(Brand, Model, Year, LicensePlate, null);

        Assert.NotNull(vehicle.UpdatedAt);
        Assert.InRange(vehicle.UpdatedAt!.Value, before, DateTime.UtcNow);
    }

    [Fact]
    public void Update_WithUpdatedUserId_SetsLastUpdatedUserId()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate);
        var userId = Guid.NewGuid();

        vehicle.Update(Brand, Model, Year, LicensePlate, null, userId);

        Assert.Equal(userId, vehicle.LastUpdatedUserId);
    }

    [Fact]
    public void Update_WithoutUpdatedUserId_LeavesLastUpdatedUserIdNull()
    {
        var vehicle = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate);

        vehicle.Update(Brand, Model, Year, LicensePlate, null);

        Assert.Null(vehicle.LastUpdatedUserId);
    }

    [Fact]
    public void TwoVehicles_HaveDifferentIds()
    {
        var v1 = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate);
        var v2 = new Vehicle(CustomerId, Brand, Model, Year, LicensePlate);

        Assert.NotEqual(v1.Id, v2.Id);
    }
}
