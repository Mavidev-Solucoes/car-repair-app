using CarRepairShop.Domain.Entities;

namespace CarRepairShop.UnitTests.Customers;

public class CustomerEntityTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321");

        Assert.Equal("John Doe", customer.Name);
        Assert.Equal("john@example.com", customer.Email);
    }

    [Fact]
    public void Constructor_StripsNonDigitsFromPersonalId()
    {
        var customer = new Customer("John Doe", "529.982.247-25", "john@example.com", "11987654321");

        Assert.Equal("52998224725", customer.PersonalId);
    }

    [Fact]
    public void Constructor_StripsNonDigitsFromTelephone()
    {
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "(11) 98765-4321");

        Assert.Equal("11987654321", customer.Telephone);
    }

    [Fact]
    public void Constructor_SetsCreatedUserId_WhenProvided()
    {
        var userId = Guid.NewGuid();

        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321", userId);

        Assert.Equal(userId, customer.CreatedUserId);
    }

    [Fact]
    public void Constructor_DoesNotSetCreatedUserId_WhenNull()
    {
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321");

        Assert.Null(customer.CreatedUserId);
    }

    [Fact]
    public void Constructor_AssignsNewId()
    {
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321");

        Assert.NotEqual(Guid.Empty, customer.Id);
    }

    [Fact]
    public void Constructor_SetsCreatedAt()
    {
        var before = DateTime.UtcNow;
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321");
        var after = DateTime.UtcNow;

        Assert.InRange(customer.CreatedAt, before, after);
    }

    [Fact]
    public void Update_UpdatesNameEmailAndTelephone()
    {
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321");

        customer.Update("Jane Smith", "jane@example.com", "11912345678");

        Assert.Equal("Jane Smith", customer.Name);
        Assert.Equal("jane@example.com", customer.Email);
        Assert.Equal("11912345678", customer.Telephone);
    }

    [Fact]
    public void Update_StripsNonDigitsFromTelephone()
    {
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321");

        customer.Update("Jane Smith", "jane@example.com", "(11) 91234-5678");

        Assert.Equal("11912345678", customer.Telephone);
    }

    [Fact]
    public void Update_SetsLastUpdatedUserId_WhenProvided()
    {
        var userId = Guid.NewGuid();
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321");

        customer.Update("Jane Smith", "jane@example.com", "11912345678", userId);

        Assert.Equal(userId, customer.LastUpdatedUserId);
    }

    [Fact]
    public void Update_SetsUpdatedAt()
    {
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321");
        var before = DateTime.UtcNow;

        customer.Update("Jane Smith", "jane@example.com", "11912345678");

        var after = DateTime.UtcNow;
        Assert.NotNull(customer.UpdatedAt);
        Assert.InRange(customer.UpdatedAt!.Value, before, after);
    }

    [Fact]
    public void Vehicles_InitiallyEmpty()
    {
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321");

        Assert.Empty(customer.Vehicles);
    }
}
