using CarRepairShop.Repository.Repositories;

namespace CarRepairShop.UnitTests.Repository;

public class UnitOfWorkTests
{
    [Fact]
    public async Task CommitAsync_SavesChanges()
    {
        using var context = DbContextFactory.Create();
        var uow = new UnitOfWork(context);
        var employee = DbContextFactory.MakeEmployee("Dave");
        context.Users.Add(employee);

        var rowsAffected = await uow.CommitAsync();

        Assert.Equal(1, rowsAffected);
    }

    [Fact]
    public async Task CommitAsync_NoChanges_ReturnsZero()
    {
        using var context = DbContextFactory.Create();
        var uow = new UnitOfWork(context);

        var rowsAffected = await uow.CommitAsync();

        Assert.Equal(0, rowsAffected);
    }
}
