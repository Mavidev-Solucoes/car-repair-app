using CarRepairShop.Application.Auth.Commands;
using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using Moq;

namespace CarRepairShop.UnitTests.Application.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsLoginResponseDto()
    {
        var user = new Employee("Alice", "alice@example.com", "hashed", UserRole.Admin);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify("password123", "hashed")).Returns(true);
        _jwtServiceMock.Setup(j => j.GenerateToken(user)).Returns("jwt-token");

        var result = await _handler.Handle(new LoginCommand("alice@example.com", "password123"), CancellationToken.None);

        Assert.Equal("jwt-token", result.Token);
        Assert.Equal("alice@example.com", result.Email);
        Assert.Equal("Alice", result.Name);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsBusinessException()
    {
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new LoginCommand("nobody@example.com", "pass"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsBusinessException()
    {
        var user = new Employee("Alice", "alice@example.com", "hashed", UserRole.Admin);
        user.Deactivate();
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new LoginCommand("alice@example.com", "password123"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsBusinessException()
    {
        var user = new Employee("Alice", "alice@example.com", "hashed", UserRole.Admin);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify("wrong", "hashed")).Returns(false);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new LoginCommand("alice@example.com", "wrong"), CancellationToken.None));
    }
}
