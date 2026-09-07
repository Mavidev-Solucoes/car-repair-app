using CarRepairShop.API.Controllers;
using CarRepairShop.Application.Auth.Commands;
using CarRepairShop.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_mediatorMock.Object);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        var command = new LoginCommand("12345678909");
        var response = new LoginResponseDto("jwt-token", "12345678909", "User Name", "Customer");
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.Login(command, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }
}
