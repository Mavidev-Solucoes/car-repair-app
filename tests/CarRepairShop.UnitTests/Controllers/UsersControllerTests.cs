using CarRepairShop.API.Controllers;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.Users.Commands;
using CarRepairShop.Application.Users.Queries;
using CarRepairShop.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_mediatorMock.Object);
    }

    private static UserDto SampleDto(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "Alice", "alice@example.com", UserRole.Admin, true, DateTime.UtcNow);

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var expected = new PagedResult<UserDto>([], 0, 1, 10);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll(new GetUsersQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = SampleDto(id);
        _mediatorMock.Setup(m => m.Send(It.Is<GetUserByIdQuery>(q => q.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction()
    {
        var command = new CreateUserCommand("Alice", "alice@example.com", "Pass@word1", UserRole.Admin);
        var dto = SampleDto();
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Create(command, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetById), created.ActionName);
        Assert.Equal(dto, created.Value);
    }

    [Fact]
    public async Task Update_WithMatchingIds_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var command = new UpdateUserCommand(id, "Alice Updated", "alice2@example.com", UserRole.Mechanic);
        var dto = SampleDto(id);
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Update(id, command, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task Update_WithMismatchedIds_ReturnsBadRequest()
    {
        var routeId = Guid.NewGuid();
        var command = new UpdateUserCommand(Guid.NewGuid(), "Alice", "alice@example.com", UserRole.Admin);

        var result = await _controller.Update(routeId, command, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _mediatorMock.Verify(m => m.Send(It.IsAny<IRequest<UserDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_WithMatchingIds_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var command = new ChangePasswordCommand(id, "OldPass@1", "NewPass@1");
        _mediatorMock.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.ChangePassword(id, command, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WithMismatchedIds_ReturnsBadRequest()
    {
        var routeId = Guid.NewGuid();
        var command = new ChangePasswordCommand(Guid.NewGuid(), "OldPass@1", "NewPass@1");

        var result = await _controller.ChangePassword(routeId, command, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _mediatorMock.Verify(m => m.Send(It.IsAny<IRequest<Unit>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deactivate_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.Is<DeactivateUserCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.Deactivate(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Activate_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.Is<ActivateUserCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await _controller.Activate(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }
}
