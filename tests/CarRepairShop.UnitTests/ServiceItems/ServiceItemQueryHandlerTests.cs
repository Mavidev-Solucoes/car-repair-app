using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.ServiceItems.Queries;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace CarRepairShop.UnitTests.ServiceItems;

public class ServiceItemQueryHandlerTests
{
    private readonly IServiceItemRepository _repository;

    public ServiceItemQueryHandlerTests()
    {
        _repository = Substitute.For<IServiceItemRepository>();
    }

    // --- GetServiceItemByIdQueryHandler ---

    [Fact]
    public async Task GetByIdHandler_WhenItemNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).ReturnsNull();

        var handler = new GetServiceItemByIdQueryHandler(_repository);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetServiceItemByIdQuery(id), CancellationToken.None));

        Assert.Contains(id.ToString(), ex.Message);
    }

    [Fact]
    public async Task GetByIdHandler_WhenItemFound_ReturnsDto()
    {
        var createdUserId = Guid.NewGuid();
        var serviceItem = new ServiceItem("Oil Change", "Full synthetic oil change", 5000, 0, createdUserId);
        _repository.GetByIdAsync(serviceItem.Id, Arg.Any<CancellationToken>()).Returns(serviceItem);

        var handler = new GetServiceItemByIdQueryHandler(_repository);

        var result = await handler.Handle(new GetServiceItemByIdQuery(serviceItem.Id), CancellationToken.None);

        Assert.Equal(serviceItem.Id, result.Id);
        Assert.Equal("Oil Change", result.Name);
        Assert.Equal("Full synthetic oil change", result.Description);
        Assert.Equal(5000, result.Price);
        Assert.Equal(createdUserId, result.CreatedUserId);
    }

    // --- GetServiceItemsQueryHandler ---

    [Fact]
    public async Task GetPagedHandler_ReturnsPagedResult()
    {
        var items = new List<ServiceItem>
        {
            new ServiceItem("Oil Change", "Description 1", 5000, 0),
            new ServiceItem("Tire Rotation", "Description 2", 2000, 0),
        };
        _repository.GetPagedAsync(1, 10, null, false, Arg.Any<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceItem, bool>>>>(), Arg.Any<CancellationToken>())
            .Returns((items.AsEnumerable(), 2));

        var handler = new GetServiceItemsQueryHandler(_repository);
        var query = new GetServiceItemsQuery { Page = 1, PageSize = 10 };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetPagedHandler_MapsItemsToDto()
    {
        var serviceItem = new ServiceItem("Brake Inspection", "Inspect brake pads", 1500, 0);
        _repository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceItem, bool>>>>(), Arg.Any<CancellationToken>())
            .Returns((new[] { serviceItem }.AsEnumerable(), 1));

        var handler = new GetServiceItemsQueryHandler(_repository);
        var query = new GetServiceItemsQuery { Page = 1, PageSize = 10 };

        var result = await handler.Handle(query, CancellationToken.None);

        var dto = result.Items.Single();
        Assert.Equal(serviceItem.Id, dto.Id);
        Assert.Equal("Brake Inspection", dto.Name);
        Assert.Equal("Inspect brake pads", dto.Description);
        Assert.Equal(1500, dto.Price);
    }

    [Fact]
    public async Task GetPagedHandler_WithNameFilter_PassesFilterToRepository()
    {
        _repository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceItem, bool>>>>(), Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<ServiceItem>(), 0));

        var handler = new GetServiceItemsQueryHandler(_repository);
        var query = new GetServiceItemsQuery { Page = 1, PageSize = 10, Name = "Oil" };

        await handler.Handle(query, CancellationToken.None);

        await _repository.Received(1).GetPagedAsync(
            1, 10, null, false,
            Arg.Is<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceItem, bool>>>>(
                filters => filters.Any()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedHandler_WithNoNameFilter_PassesEmptyFiltersToRepository()
    {
        _repository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceItem, bool>>>>(), Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<ServiceItem>(), 0));

        var handler = new GetServiceItemsQueryHandler(_repository);
        var query = new GetServiceItemsQuery { Page = 1, PageSize = 10 };

        await handler.Handle(query, CancellationToken.None);

        await _repository.Received(1).GetPagedAsync(
            1, 10, null, false,
            Arg.Is<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceItem, bool>>>>(
                filters => !filters.Any()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedHandler_WithOrderByAndDescending_PassesParamsToRepository()
    {
        _repository.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceItem, bool>>>>(), Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<ServiceItem>(), 0));

        var handler = new GetServiceItemsQueryHandler(_repository);
        var query = new GetServiceItemsQuery { Page = 2, PageSize = 5, OrderBy = "Name", OrderDescending = true };

        await handler.Handle(query, CancellationToken.None);

        await _repository.Received(1).GetPagedAsync(
            2, 5, "Name", true,
            Arg.Any<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceItem, bool>>>>(),
            Arg.Any<CancellationToken>());
    }
}
