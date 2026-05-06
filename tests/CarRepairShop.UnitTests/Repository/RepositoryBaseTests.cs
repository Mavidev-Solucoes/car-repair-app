using CarRepairShop.Domain.Entities;
using CarRepairShop.Repository.Repositories;

namespace CarRepairShop.UnitTests.Repository;

public class RepositoryBaseTests
{
    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        using var context = DbContextFactory.Create();
        var employee = DbContextFactory.MakeEmployee();
        context.Users.Add(employee);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var found = await repo.GetByIdAsync(employee.Id);

        Assert.NotNull(found);
        Assert.Equal(employee.Id, found!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotExisting_ReturnsNull()
    {
        using var context = DbContextFactory.Create();
        var repo = new UserRepository(context);

        var found = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        using var context = DbContextFactory.Create();
        context.Users.Add(DbContextFactory.MakeEmployee("Bob"));
        context.Users.Add(DbContextFactory.MakeEmployee("Carol"));
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var all = await repo.GetAllAsync();

        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task AddAsync_PersistsEntity()
    {
        using var context = DbContextFactory.Create();
        var repo = new UserRepository(context);
        var employee = DbContextFactory.MakeEmployee("Dave");

        await repo.AddAsync(employee);
        await context.SaveChangesAsync();

        Assert.Equal(1, context.Users.Count());
    }

    [Fact]
    public async Task Update_ModifiesEntity()
    {
        using var context = DbContextFactory.Create();
        var item = DbContextFactory.MakeServiceItem("Pads");
        context.ServiceItems.Add(item);
        await context.SaveChangesAsync();

        var repo = new ServiceItemRepository(context);
        item.Update("Brake Pads", "Updated desc", 75m, 5);
        repo.Update(item);
        await context.SaveChangesAsync();

        var updated = await context.ServiceItems.FindAsync(item.Id);
        Assert.Equal("Brake Pads", updated!.Name);
    }

    [Fact]
    public async Task Delete_RemovesEntity()
    {
        using var context = DbContextFactory.Create();
        var item = DbContextFactory.MakeServiceItem();
        context.ServiceItems.Add(item);
        await context.SaveChangesAsync();

        var repo = new ServiceItemRepository(context);
        repo.Delete(item);
        await context.SaveChangesAsync();

        Assert.Equal(0, context.ServiceItems.Count());
    }
}
