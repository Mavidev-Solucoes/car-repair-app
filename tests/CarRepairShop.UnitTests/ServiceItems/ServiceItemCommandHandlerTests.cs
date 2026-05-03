using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.ServiceItems.Commands;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace CarRepairShop.UnitTests.ServiceItems;

public class ServiceItemCommandHandlerTests
{
    private readonly IServiceItemRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ServiceItemCommandHandlerTests()
    {
        _repository = Substitute.For<IServiceItemRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUserService = Substitute.For<ICurrentUserService>();
    }

    // --- CreateServiceItemCommandHandler ---

    [Fact]
    public async Task CreateHandler_WhenNameAlreadyExists_ThrowsBusinessException()
    {
        var command = new CreateServiceItemCommand("Oil Change", "Description", 5000, 0);
        _repository.ExistsByNameAsync(command.Name, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateServiceItemCommandHandler(_repository, _unitOfWork, _currentUserService);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("Oil Change", ex.Message);
    }

    [Fact]
    public async Task CreateHandler_WhenNameDoesNotExist_CreatesAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        var command = new CreateServiceItemCommand("Tire Rotation", "Rotate all four tires", 2000, 0);
        _repository.ExistsByNameAsync(command.Name, Arg.Any<CancellationToken>()).Returns(false);
        _currentUserService.UserId.Returns(userId);

        var handler = new CreateServiceItemCommandHandler(_repository, _unitOfWork, _currentUserService);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Tire Rotation", result.Name);
        Assert.Equal("Rotate all four tires", result.Description);
        Assert.Equal(2000, result.Price);
        Assert.Equal(userId, result.CreatedUserId);
        Assert.NotEqual(Guid.Empty, result.Id);

        await _repository.Received(1).AddAsync(Arg.Any<ServiceItem>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateHandler_WhenNameDoesNotExist_WithNullUserId_CreatesItem()
    {
        var command = new CreateServiceItemCommand("Brake Inspection", "Inspect brake pads", 1500, 0);
        _repository.ExistsByNameAsync(command.Name, Arg.Any<CancellationToken>()).Returns(false);
        _currentUserService.UserId.Returns((Guid?)null);

        var handler = new CreateServiceItemCommandHandler(_repository, _unitOfWork, _currentUserService);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Null(result.CreatedUserId);
        Assert.Equal("Brake Inspection", result.Name);
    }

    // --- UpdateServiceItemCommandHandler ---

    [Fact]
    public async Task UpdateHandler_WhenItemNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        var command = new UpdateServiceItemCommand(id, "New Name", "New Description", 3000, 0);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).ReturnsNull();

        var handler = new UpdateServiceItemCommandHandler(_repository, _unitOfWork, _currentUserService);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains(id.ToString(), ex.Message);
    }

    [Fact]
    public async Task UpdateHandler_WhenItemFound_UpdatesAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        var serviceItem = new ServiceItem("Old Name", "Old Description", 1000, 0);
        var command = new UpdateServiceItemCommand(serviceItem.Id, "New Name", "New Description", 3000, 0);
        _repository.GetByIdAsync(serviceItem.Id, Arg.Any<CancellationToken>()).Returns(serviceItem);
        _currentUserService.UserId.Returns(userId);

        var handler = new UpdateServiceItemCommandHandler(_repository, _unitOfWork, _currentUserService);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("New Name", result.Name);
        Assert.Equal("New Description", result.Description);
        Assert.Equal(3000, result.Price);
        Assert.Equal(userId, result.LastUpdatedUserId);

        _repository.Received(1).Update(serviceItem);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // --- DeleteServiceItemCommandHandler ---

    [Fact]
    public async Task DeleteHandler_WhenItemNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        var command = new DeleteServiceItemCommand(id);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).ReturnsNull();

        var handler = new DeleteServiceItemCommandHandler(_repository, _unitOfWork);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains(id.ToString(), ex.Message);
    }

    [Fact]
    public async Task DeleteHandler_WhenItemFound_DeletesAndReturnsUnit()
    {
        var serviceItem = new ServiceItem("Oil Change", "Description", 5000, 0);
        var command = new DeleteServiceItemCommand(serviceItem.Id);
        _repository.GetByIdAsync(serviceItem.Id, Arg.Any<CancellationToken>()).Returns(serviceItem);

        var handler = new DeleteServiceItemCommandHandler(_repository, _unitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(MediatR.Unit.Value, result);
        _repository.Received(1).Delete(serviceItem);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
