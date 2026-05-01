using CarRepairShop.Domain.Entities;

namespace CarRepairShop.UnitTests.ServiceItems;

public class ServiceItemEntityTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsPropertiesCorrectly()
    {
        var createdUserId = Guid.NewGuid();

        var serviceItem = new ServiceItem("Oil Change", "Full synthetic oil change", 5000, createdUserId);

        Assert.Equal("Oil Change", serviceItem.Name);
        Assert.Equal("Full synthetic oil change", serviceItem.Description);
        Assert.Equal(5000, serviceItem.UnitCost);
        Assert.Equal(createdUserId, serviceItem.CreatedUserId);
        Assert.NotEqual(Guid.Empty, serviceItem.Id);
        Assert.True(serviceItem.CreatedAt <= DateTime.UtcNow);
        Assert.Null(serviceItem.UpdatedAt);
        Assert.Null(serviceItem.LastUpdatedUserId);
    }

    [Fact]
    public void Constructor_WithNullCreatedUserId_LeavesCreatedUserIdNull()
    {
        var serviceItem = new ServiceItem("Tire Rotation", "Rotate all four tires", 2000);

        Assert.Null(serviceItem.CreatedUserId);
    }

    [Fact]
    public void Constructor_GeneratesUniqueIds()
    {
        var item1 = new ServiceItem("Item A", "Description A", 100);
        var item2 = new ServiceItem("Item B", "Description B", 200);

        Assert.NotEqual(item1.Id, item2.Id);
    }

    [Fact]
    public void Update_WithValidArguments_UpdatesPropertiesCorrectly()
    {
        var serviceItem = new ServiceItem("Old Name", "Old Description", 1000);
        var updatedUserId = Guid.NewGuid();

        serviceItem.Update("New Name", "New Description", 2000, updatedUserId);

        Assert.Equal("New Name", serviceItem.Name);
        Assert.Equal("New Description", serviceItem.Description);
        Assert.Equal(2000, serviceItem.UnitCost);
        Assert.Equal(updatedUserId, serviceItem.LastUpdatedUserId);
        Assert.NotNull(serviceItem.UpdatedAt);
        Assert.True(serviceItem.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Update_WithNullUpdatedUserId_SetsLastUpdatedUserIdToNull()
    {
        var serviceItem = new ServiceItem("Name", "Description", 1000);

        serviceItem.Update("New Name", "New Description", 2000, null);

        Assert.Null(serviceItem.LastUpdatedUserId);
        Assert.NotNull(serviceItem.UpdatedAt);
    }

    [Fact]
    public void Update_DoesNotChangeCreatedUserId()
    {
        var createdUserId = Guid.NewGuid();
        var serviceItem = new ServiceItem("Name", "Description", 1000, createdUserId);

        serviceItem.Update("New Name", "New Description", 2000, Guid.NewGuid());

        Assert.Equal(createdUserId, serviceItem.CreatedUserId);
    }

    [Fact]
    public void Update_DoesNotChangeCreatedAt()
    {
        var serviceItem = new ServiceItem("Name", "Description", 1000);
        var originalCreatedAt = serviceItem.CreatedAt;

        serviceItem.Update("New Name", "New Description", 2000);

        Assert.Equal(originalCreatedAt, serviceItem.CreatedAt);
    }
}
