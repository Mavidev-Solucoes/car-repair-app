using CarRepairShop.Domain.Entities;

namespace CarRepairShop.UnitTests.Domain;

public class ServiceJobTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var createdUserId = Guid.NewGuid();

        var job = new ServiceJob("Oil Change", "Engine oil change", 5000m, createdUserId);

        Assert.Equal("Oil Change", job.Name);
        Assert.Equal("Engine oil change", job.Description);
        Assert.Equal(5000m, job.Price);
        Assert.Equal(createdUserId, job.CreatedUserId);
    }

    [Fact]
    public void Constructor_WithoutCreatedUserId_LeavesCreatedUserIdNull()
    {
        var job = new ServiceJob("Oil Change", "Engine oil change", 5000m);

        Assert.Null(job.CreatedUserId);
    }

    [Fact]
    public void Constructor_GeneratesUniqueIds()
    {
        var job1 = new ServiceJob("Job 1", "Desc 1", 1000m);
        var job2 = new ServiceJob("Job 2", "Desc 2", 2000m);

        Assert.NotEqual(job1.Id, job2.Id);
    }

    [Fact]
    public void Update_ChangesNameDescriptionAndPrice()
    {
        var updatedUserId = Guid.NewGuid();
        var job = new ServiceJob("Old Name", "Old Desc", 100m);

        job.Update("New Name", "New Desc", 200m, updatedUserId);

        Assert.Equal("New Name", job.Name);
        Assert.Equal("New Desc", job.Description);
        Assert.Equal(200m, job.Price);
        Assert.Equal(updatedUserId, job.LastUpdatedUserId);
        Assert.NotNull(job.UpdatedAt);
    }
}
