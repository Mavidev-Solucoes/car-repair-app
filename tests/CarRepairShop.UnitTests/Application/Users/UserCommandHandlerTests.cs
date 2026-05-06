using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.Users.Commands;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;
using Moq;

namespace CarRepairShop.UnitTests.Application.Users;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _handler = new CreateUserCommandHandler(_userRepoMock.Object, _passwordHasherMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserAndReturnsDto()
    {
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.Hash("Pass@word1")).Returns("hashed");

        var command = new CreateUserCommand("Alice", "alice@example.com", "Pass@word1", UserRole.Admin);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Alice", result.Name);
        Assert.Equal("alice@example.com", result.Email);
        Assert.Equal(UserRole.Admin, result.Role);
        Assert.True(result.IsActive);
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsBusinessException()
    {
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateUserCommand("Alice", "alice@example.com", "Pass@word1", UserRole.Admin);

        await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class UpdateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _handler = new UpdateUserCommandHandler(_userRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesAndReturnsDto()
    {
        var user = new Employee("Alice", "alice@example.com", "hashed", UserRole.Admin);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = new UpdateUserCommand(user.Id, "Alice Updated", "alice@example.com", UserRole.Mechanic);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Alice Updated", result.Name);
        Assert.Equal(UserRole.Mechanic, result.Role);
        _userRepoMock.Verify(r => r.Update(user), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new UpdateUserCommand(Guid.NewGuid(), "Name", "email@example.com", UserRole.Admin), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmailChangedToDuplicate_ThrowsBusinessException()
    {
        var user = new Employee("Alice", "alice@example.com", "hashed", UserRole.Admin);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock.Setup(r => r.ExistsByEmailAsync("other@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new UpdateUserCommand(user.Id, "Alice", "other@example.com", UserRole.Admin), CancellationToken.None));
    }
}

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _handler = new ChangePasswordCommandHandler(_userRepoMock.Object, _passwordHasherMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ChangesPasswordAndReturnsUnit()
    {
        var user = new Employee("Alice", "alice@example.com", "oldhash", UserRole.Admin);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify("OldPass@1", "oldhash")).Returns(true);
        _passwordHasherMock.Setup(h => h.Hash("NewPass@1")).Returns("newhash");

        var result = await _handler.Handle(new ChangePasswordCommand(user.Id, "OldPass@1", "NewPass@1"), CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new ChangePasswordCommand(Guid.NewGuid(), "old", "new"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ThrowsBusinessException()
    {
        var user = new Employee("Alice", "alice@example.com", "oldhash", UserRole.Admin);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify("WrongPass", "oldhash")).Returns(false);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new ChangePasswordCommand(user.Id, "WrongPass", "NewPass@1"), CancellationToken.None));
    }
}

public class DeactivateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeactivateUserCommandHandler _handler;

    public DeactivateUserCommandHandlerTests()
    {
        _handler = new DeactivateUserCommandHandler(_userRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_DeactivatesAndReturnsUnit()
    {
        var user = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new DeactivateUserCommand(user.Id), CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        Assert.False(user.IsActive);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeactivateUserCommand(Guid.NewGuid()), CancellationToken.None));
    }
}

public class ActivateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ActivateUserCommandHandler _handler;

    public ActivateUserCommandHandlerTests()
    {
        _handler = new ActivateUserCommandHandler(_userRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_InactiveUser_ActivatesAndReturnsUnit()
    {
        var user = new Employee("Alice", "alice@example.com", "hash", UserRole.Admin);
        user.Deactivate();
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new ActivateUserCommand(user.Id), CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        Assert.True(user.IsActive);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new ActivateUserCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
