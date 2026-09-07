using CarRepairShop.Application.Auth.Commands;
using CarRepairShop.Domain.Interfaces.Services;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CarRepairShop.UnitTests.Application.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(_jwtServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCpf_ReturnsLoginResponseDto()
    {
        var token = CreateToken("John Doe", "Customer");
        _jwtServiceMock
            .Setup(service => service.GenerateTokenAsync("12345678909", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var result = await _handler.Handle(new LoginCommand("12345678909"), CancellationToken.None);

        Assert.Equal(token, result.Token);
        Assert.Equal("12345678909", result.Email);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("Customer", result.Role);
    }

    [Fact]
    public async Task Handle_LambdaFailure_ThrowsInvalidOperationException()
    {
        _jwtServiceMock
            .Setup(service => service.GenerateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Lambda auth failed."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new LoginCommand("12345678909"), CancellationToken.None));
    }

    private static string CreateToken(string name, string role)
    {
        var token = new JwtSecurityToken(
            claims: new[]
            {
                new Claim("name", name),
                new Claim("role", role)
            });

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
