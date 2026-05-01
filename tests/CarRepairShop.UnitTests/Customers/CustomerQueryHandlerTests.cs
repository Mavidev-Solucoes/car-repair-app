using System.Linq.Expressions;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.Customers.Queries;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using Moq;

namespace CarRepairShop.UnitTests.Customers;

public class GetCustomerByIdQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly GetCustomerByIdQueryHandler _handler;

    public GetCustomerByIdQueryHandlerTests()
    {
        _handler = new GetCustomerByIdQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_ReturnsDto()
    {
        var customerId = Guid.NewGuid();
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321");
        _repositoryMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _handler.Handle(new GetCustomerByIdQuery(customerId), CancellationToken.None);

        Assert.Equal("John Doe", result.Name);
        Assert.Equal("52998224725", result.PersonalId);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("11987654321", result.Telephone);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(new GetCustomerByIdQuery(customerId), CancellationToken.None));
    }
}

public class GetCustomerWithVehiclesQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly GetCustomerWithVehiclesQueryHandler _handler;

    public GetCustomerWithVehiclesQueryHandlerTests()
    {
        _handler = new GetCustomerWithVehiclesQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_ReturnsDto()
    {
        var customerId = Guid.NewGuid();
        var customer = new Customer("Jane Smith", "52998224725", "jane@example.com", "11912345678");
        _repositoryMock
            .Setup(r => r.GetWithVehiclesAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _handler.Handle(new GetCustomerWithVehiclesQuery(customerId), CancellationToken.None);

        Assert.Equal("Jane Smith", result.Name);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetWithVehiclesAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(new GetCustomerWithVehiclesQuery(customerId), CancellationToken.None));
    }
}

public class GetCustomersQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly GetCustomersQueryHandler _handler;

    public GetCustomersQueryHandlerTests()
    {
        _handler = new GetCustomersQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        var customers = new List<Customer>
        {
            new("John Doe", "52998224725", "john@example.com", "11987654321"),
            new("Jane Smith", "07132762460", "jane@example.com", "11912345678")
        };

        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<IEnumerable<Expression<Func<Customer, bool>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((customers, 2));

        var query = new GetCustomersQuery { Page = 1, PageSize = 10 };

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyPagedResult()
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<IEnumerable<Expression<Func<Customer, bool>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Customer>(), 0));

        var query = new GetCustomersQuery { Page = 1, PageSize = 10 };

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Handle_WithNameFilter_PassesFilterToRepository()
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<IEnumerable<Expression<Func<Customer, bool>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Customer>(), 0));

        var query = new GetCustomersQuery { Page = 1, PageSize = 10, Name = "John" };

        await _handler.Handle(query, CancellationToken.None);

        _repositoryMock.Verify(r => r.GetPagedAsync(
            1, 10, null, false,
            It.Is<IEnumerable<Expression<Func<Customer, bool>>>>(filters => filters.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmailFilter_PassesFilterToRepository()
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<IEnumerable<Expression<Func<Customer, bool>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Customer>(), 0));

        var query = new GetCustomersQuery { Page = 1, PageSize = 10, Email = "john@" };

        await _handler.Handle(query, CancellationToken.None);

        _repositoryMock.Verify(r => r.GetPagedAsync(
            1, 10, null, false,
            It.Is<IEnumerable<Expression<Func<Customer, bool>>>>(filters => filters.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPersonalIdFilter_PassesFilterToRepository()
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<IEnumerable<Expression<Func<Customer, bool>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Customer>(), 0));

        var query = new GetCustomersQuery { Page = 1, PageSize = 10, PersonalId = "529.982.247-25" };

        await _handler.Handle(query, CancellationToken.None);

        _repositoryMock.Verify(r => r.GetPagedAsync(
            1, 10, null, false,
            It.Is<IEnumerable<Expression<Func<Customer, bool>>>>(filters => filters.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTelephoneFilter_PassesFilterToRepository()
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<IEnumerable<Expression<Func<Customer, bool>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Customer>(), 0));

        var query = new GetCustomersQuery { Page = 1, PageSize = 10, Telephone = "(11) 98765-4321" };

        await _handler.Handle(query, CancellationToken.None);

        _repositoryMock.Verify(r => r.GetPagedAsync(
            1, 10, null, false,
            It.Is<IEnumerable<Expression<Func<Customer, bool>>>>(filters => filters.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoFilters_PassesEmptyFiltersToRepository()
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<IEnumerable<Expression<Func<Customer, bool>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Customer>(), 0));

        var query = new GetCustomersQuery { Page = 2, PageSize = 5, OrderBy = "Name", OrderDescending = true };

        await _handler.Handle(query, CancellationToken.None);

        _repositoryMock.Verify(r => r.GetPagedAsync(
            2, 5, "Name", true,
            It.Is<IEnumerable<Expression<Func<Customer, bool>>>>(filters => !filters.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsCustomersToDtos()
    {
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321");
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<IEnumerable<Expression<Func<Customer, bool>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Customer> { customer }, 1));

        var query = new GetCustomersQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        var dto = result.Items.Single();
        Assert.Equal("John Doe", dto.Name);
        Assert.Equal("52998224725", dto.PersonalId);
        Assert.Equal("john@example.com", dto.Email);
        Assert.Equal("11987654321", dto.Telephone);
    }
}
