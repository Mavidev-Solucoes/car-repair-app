using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.ServiceItems.Commands;
using CarRepairShop.Application.ServiceItems.Queries;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;
using Moq;

namespace CarRepairShop.UnitTests.Application.ServiceItems;

public class CreateServiceItemCommandHandlerTests
{
    private readonly Mock<IServiceItemRepository> _serviceItemRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly CreateServiceItemCommandHandler _handler;

    public CreateServiceItemCommandHandlerTests()
    {
        _handler = new CreateServiceItemCommandHandler(
            _serviceItemRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesServiceItemAndReturnsDto()
    {
        _serviceItemRepoMock.Setup(r => r.ExistsByNameAsync("Oil Change", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _currentUserMock.Setup(s => s.UserId).Returns(Guid.NewGuid());

        var command = new CreateServiceItemCommand("Oil Change", "Change oil filter", 150m, 10);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Oil Change", result.Name);
        Assert.Equal(150m, result.Price);
        Assert.Equal(10, result.Stock);
        _serviceItemRepoMock.Verify(r => r.AddAsync(It.IsAny<ServiceItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsBusinessException()
    {
        _serviceItemRepoMock.Setup(r => r.ExistsByNameAsync("Oil Change", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateServiceItemCommand("Oil Change", "Change oil filter", 150m, 10);
        await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
    }
}

public class UpdateServiceItemCommandHandlerTests
{
    private readonly Mock<IServiceItemRepository> _serviceItemRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly UpdateServiceItemCommandHandler _handler;

    public UpdateServiceItemCommandHandlerTests()
    {
        _handler = new UpdateServiceItemCommandHandler(
            _serviceItemRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesServiceItemAndReturnsDto()
    {
        var item = new ServiceItem("Oil Change", "Old desc", 100m, 5);
        _serviceItemRepoMock.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _serviceItemRepoMock.Setup(r => r.ExistsByNameAsync("Oil Change", item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new UpdateServiceItemCommand(item.Id, "Oil Change", "New desc", 120m, 8);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Oil Change", result.Name);
        Assert.Equal(120m, result.Price);
        Assert.Equal(8, result.Stock);
        _serviceItemRepoMock.Verify(r => r.Update(item), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        _serviceItemRepoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _serviceItemRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new UpdateServiceItemCommand(Guid.NewGuid(), "Oil Change", "Desc", 100m, 5), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsBusinessException()
    {
        var id = Guid.NewGuid();
        _serviceItemRepoMock.Setup(r => r.ExistsByNameAsync("Duplicate Name", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new UpdateServiceItemCommand(id, "Duplicate Name", "Desc", 100m, 5), CancellationToken.None));
    }
}

public class DeleteServiceItemCommandHandlerTests
{
    private readonly Mock<IServiceItemRepository> _serviceItemRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteServiceItemCommandHandler _handler;

    public DeleteServiceItemCommandHandlerTests()
    {
        _handler = new DeleteServiceItemCommandHandler(_serviceItemRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidItem_DeletesAndReturnsUnit()
    {
        var item = new ServiceItem("Oil Change", "Desc", 100m, 5);
        _serviceItemRepoMock.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _handler.Handle(new DeleteServiceItemCommand(item.Id), CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        _serviceItemRepoMock.Verify(r => r.Delete(item), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        _serviceItemRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeleteServiceItemCommand(Guid.NewGuid()), CancellationToken.None));
    }
}

public class GetServiceItemByIdQueryHandlerTests
{
    private readonly Mock<IServiceItemRepository> _serviceItemRepoMock = new();
    private readonly GetServiceItemByIdQueryHandler _handler;

    public GetServiceItemByIdQueryHandlerTests()
    {
        _handler = new GetServiceItemByIdQueryHandler(_serviceItemRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ItemExists_ReturnsDto()
    {
        var item = new ServiceItem("Oil Change", "Desc", 100m, 5);
        _serviceItemRepoMock.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var result = await _handler.Handle(new GetServiceItemByIdQuery(item.Id), CancellationToken.None);

        Assert.Equal(item.Id, result.Id);
        Assert.Equal("Oil Change", result.Name);
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        _serviceItemRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetServiceItemByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}

public class GetServiceItemsQueryHandlerTests
{
    private readonly Mock<IServiceItemRepository> _serviceItemRepoMock = new();
    private readonly GetServiceItemsQueryHandler _handler;

    public GetServiceItemsQueryHandlerTests()
    {
        _handler = new GetServiceItemsQueryHandler(_serviceItemRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        var items = new List<ServiceItem> { new("Oil Change", "Desc", 100m, 5) };
        _serviceItemRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceItem, bool>>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        var result = await _handler.Handle(new GetServiceItemsQuery { Page = 1, PageSize = 10 }, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }
}
