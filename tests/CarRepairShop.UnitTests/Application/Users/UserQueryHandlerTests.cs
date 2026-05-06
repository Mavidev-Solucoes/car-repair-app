using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.Users.Queries;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using Moq;

namespace CarRepairShop.UnitTests.Application.Users;

public class GetUserByIdQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _handler = new GetUserByIdQueryHandler(_userRepoMock.Object);
    }

    [Fact]
    public async Task Handle_UserExists_ReturnsDto()
    {
        var user = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal("Alice", result.Name);
        Assert.Equal(UserRole.Admin, result.Role);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly GetUsersQueryHandler _handler;

    public GetUsersQueryHandlerTests()
    {
        _handler = new GetUsersQueryHandler(_userRepoMock.Object);
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsAllUsers()
    {
        var users = new List<User>
        {
            new Employee("Alice", "alice@example.com", "hash", UserRole.Admin),
            new Employee("Bob", "bob@example.com", "hash", UserRole.Mechanic)
        };
        _userRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _handler.Handle(new GetUsersQuery { Page = 1, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task Handle_SearchFilter_ReturnsFilteredUsers()
    {
        var users = new List<User>
        {
            new Employee("Alice", "alice@example.com", "hash", UserRole.Admin),
            new Employee("Bob", "bob@example.com", "hash", UserRole.Mechanic)
        };
        _userRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _handler.Handle(new GetUsersQuery { Search = "Alice" }, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Alice", result.Items.First().Name);
    }

    [Fact]
    public async Task Handle_RoleFilter_ReturnsFilteredUsers()
    {
        var users = new List<User>
        {
            new Employee("Alice", "alice@example.com", "hash", UserRole.Admin),
            new Employee("Bob", "bob@example.com", "hash", UserRole.Mechanic)
        };
        _userRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _handler.Handle(new GetUsersQuery { Role = UserRole.Mechanic }, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Bob", result.Items.First().Name);
    }

    [Fact]
    public async Task Handle_IsActiveFilter_ReturnsFilteredUsers()
    {
        var active = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        var inactive = new Employee("Bob", "bob@example.com", "hash", UserRole.Mechanic);
        inactive.Deactivate();
        _userRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { active, inactive });

        var result = await _handler.Handle(new GetUsersQuery { IsActive = true }, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, u => Assert.True(u.IsActive));
    }
}

public class GetAllUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly GetAllUsersQueryHandler _handler;

    public GetAllUsersQueryHandlerTests()
    {
        _handler = new GetAllUsersQueryHandler(_userRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllEmployees()
    {
        var users = new List<User>
        {
            new Employee("Alice", "alice@example.com", "hash", UserRole.Admin),
            new Employee("Bob", "bob@example.com", "hash", UserRole.Mechanic)
        };
        _userRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count());
    }
}
