using CarRepairShop.Domain.Entities;
using CarRepairShop.Repository.Repositories;

namespace CarRepairShop.UnitTests.Repository;

public class ServiceItemRepositoryTests
{
    [Fact]
    public async Task ExistsByNameAsync_Existing_ReturnsTrue()
    {
        using var context = DbContextFactory.Create();
        context.ServiceItems.Add(DbContextFactory.MakeServiceItem("Brake Pads"));
        await context.SaveChangesAsync();

        var repo = new ServiceItemRepository(context);
        var exists = await repo.ExistsByNameAsync("Brake Pads");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_CaseInsensitive_ReturnsTrue()
    {
        using var context = DbContextFactory.Create();
        context.ServiceItems.Add(DbContextFactory.MakeServiceItem("Brake Pads"));
        await context.SaveChangesAsync();

        var repo = new ServiceItemRepository(context);
        var exists = await repo.ExistsByNameAsync("brake pads");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_NotFound_ReturnsFalse()
    {
        using var context = DbContextFactory.Create();
        var repo = new ServiceItemRepository(context);

        var exists = await repo.ExistsByNameAsync("Nonexistent");

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_WithExcludingId_ExcludesSpecificItem()
    {
        using var context = DbContextFactory.Create();
        var item = DbContextFactory.MakeServiceItem("Brake Pads");
        context.ServiceItems.Add(item);
        await context.SaveChangesAsync();

        var repo = new ServiceItemRepository(context);
        // Excluding the item itself - should return false
        var exists = await repo.ExistsByNameAsync("Brake Pads", item.Id);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsByNameAsync_WithExcludingId_OtherItemWithSameName_ReturnsTrue()
    {
        using var context = DbContextFactory.Create();
        var item1 = DbContextFactory.MakeServiceItem("Brake Pads");
        var item2 = DbContextFactory.MakeServiceItem("Oil Filter");
        context.ServiceItems.Add(item1);
        context.ServiceItems.Add(item2);
        await context.SaveChangesAsync();

        var repo = new ServiceItemRepository(context);
        // Excluding item2 but item1 also has "Brake Pads" -> should return true (found in item1)
        var exists = await repo.ExistsByNameAsync("Brake Pads", item2.Id);

        Assert.True(exists);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        using var context = DbContextFactory.Create();
        context.ServiceItems.Add(new ServiceItem("Brake Pads", "Desc", 50m, 10));
        context.ServiceItems.Add(new ServiceItem("Oil Filter", "Desc", 20m, 25));
        context.ServiceItems.Add(new ServiceItem("Spark Plug", "Desc", 10m, 50));
        await context.SaveChangesAsync();

        var repo = new ServiceItemRepository(context);
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
        context.ServiceItems.Add(DbContextFactory.MakeServiceItem());
        await context.SaveChangesAsync();

        var repo = new ServiceItemRepository(context);
        var (items, _) = await repo.GetPagedAsync(1, 10, orderBy, desc);

        Assert.Single(items);
    }

    [Fact]
    public async Task GetPagedAsync_WithFilter_ReturnsFilteredResults()
    {
        using var context = DbContextFactory.Create();
        context.ServiceItems.Add(new ServiceItem("Brake Pads", "Desc", 50m, 10));
        context.ServiceItems.Add(new ServiceItem("Oil Filter", "Desc", 20m, 25));
        await context.SaveChangesAsync();

        var repo = new ServiceItemRepository(context);
        var (items, totalCount) = await repo.GetPagedAsync(
            1, 10, "name", false,
            new System.Linq.Expressions.Expression<Func<ServiceItem, bool>>[]
            {
                si => si.Name == "Brake Pads"
            });

        Assert.Equal(1, totalCount);
        Assert.Equal("Brake Pads", items.Single().Name);
    }
}
