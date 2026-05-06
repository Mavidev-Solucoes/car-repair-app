using CarRepairShop.Repository.Repositories;

namespace CarRepairShop.UnitTests.Repository;

public class UserRepositoryTests
{
    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        using var context = DbContextFactory.Create();
        var employee = DbContextFactory.MakeEmployee("Bob");
        context.Users.Add(employee);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var found = await repo.GetByEmailAsync("bob@example.com");

        Assert.NotNull(found);
        Assert.Equal("Bob", found!.Name);
    }

    [Fact]
    public async Task GetByEmailAsync_NotFound_ReturnsNull()
    {
        using var context = DbContextFactory.Create();
        var repo = new UserRepository(context);

        var found = await repo.GetByEmailAsync("nobody@example.com");

        Assert.Null(found);
    }

    [Fact]
    public async Task ExistsByEmailAsync_ExistingEmail_ReturnsTrue()
    {
        using var context = DbContextFactory.Create();
        var employee = DbContextFactory.MakeEmployee("Carol");
        context.Users.Add(employee);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var exists = await repo.ExistsByEmailAsync("carol@example.com");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsByEmailAsync_NotFound_ReturnsFalse()
    {
        using var context = DbContextFactory.Create();
        var repo = new UserRepository(context);

        var exists = await repo.ExistsByEmailAsync("nobody@example.com");

        Assert.False(exists);
    }
}
