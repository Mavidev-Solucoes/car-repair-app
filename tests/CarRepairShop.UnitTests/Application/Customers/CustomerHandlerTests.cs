using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.Customers.Commands;
using CarRepairShop.Application.Customers.Queries;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;
using Moq;

namespace CarRepairShop.UnitTests.Application.Customers;

public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _handler = new CreateCustomerCommandHandler(
            _customerRepoMock.Object,
            _passwordHasherMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesCustomerAndReturnsDto()
    {
        _customerRepoMock.Setup(r => r.ExistsByDocumentAsync("52998224725", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.Hash("Mudar@123")).Returns("hashed");

        var command = new CreateCustomerCommand("John", "52998224725", "john@example.com", "11987654321");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("John", result.Name);
        Assert.Equal("john@example.com", result.Email);
        _customerRepoMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicatePersonalId_ThrowsBusinessException()
    {
        _customerRepoMock.Setup(r => r.ExistsByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateCustomerCommand("John", "52998224725", "john@example.com", "11987654321");
        await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
    }
}

public class UpdateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly UpdateCustomerCommandHandler _handler;

    public UpdateCustomerCommandHandlerTests()
    {
        _handler = new UpdateCustomerCommandHandler(
            _customerRepoMock.Object,
            _uowMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesCustomerAndReturnsDto()
    {
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var command = new UpdateCustomerCommand(customer.Id, "Jane", "jane@example.com", "11912345678");
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Jane", result.Name);
        _customerRepoMock.Verify(r => r.Update(customer), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        _customerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new UpdateCustomerCommand(Guid.NewGuid(), "Jane", "jane@example.com", "11912345678"), CancellationToken.None));
    }
}

public class DeleteCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteCustomerCommandHandler _handler;

    public DeleteCustomerCommandHandlerTests()
    {
        _handler = new DeleteCustomerCommandHandler(_customerRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCustomer_DeletesAndReturnsUnit()
    {
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _customerRepoMock.Setup(r => r.HasServiceOrdersAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _customerRepoMock.Setup(r => r.HasVehiclesAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new DeleteCustomerCommand(customer.Id), CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        _customerRepoMock.Verify(r => r.Delete(customer), Times.Once);
        _uowMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        _customerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new DeleteCustomerCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CustomerWithServiceOrders_ThrowsBusinessException()
    {
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _customerRepoMock.Setup(r => r.HasServiceOrdersAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new DeleteCustomerCommand(customer.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CustomerWithVehicles_ThrowsBusinessException()
    {
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _customerRepoMock.Setup(r => r.HasServiceOrdersAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _customerRepoMock.Setup(r => r.HasVehiclesAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new DeleteCustomerCommand(customer.Id), CancellationToken.None));
    }
}

public class GetCustomerByIdQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly GetCustomerByIdQueryHandler _handler;

    public GetCustomerByIdQueryHandlerTests()
    {
        _handler = new GetCustomerByIdQueryHandler(_customerRepoMock.Object);
    }

    [Fact]
    public async Task Handle_CustomerExists_ReturnsDto()
    {
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        _customerRepoMock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _handler.Handle(new GetCustomerByIdQuery(customer.Id), CancellationToken.None);

        Assert.Equal(customer.Id, result.Id);
        Assert.Equal("John", result.Name);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        _customerRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetCustomerByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}

public class GetCustomerWithVehiclesQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly GetCustomerWithVehiclesQueryHandler _handler;

    public GetCustomerWithVehiclesQueryHandlerTests()
    {
        _handler = new GetCustomerWithVehiclesQueryHandler(_customerRepoMock.Object);
    }

    [Fact]
    public async Task Handle_CustomerExists_ReturnsDto()
    {
        var customer = new Customer("John", "52998224725", "john@example.com", "11987654321", "hash");
        _customerRepoMock.Setup(r => r.GetWithVehiclesAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _handler.Handle(new GetCustomerWithVehiclesQuery(customer.Id), CancellationToken.None);

        Assert.Equal(customer.Id, result.Id);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        _customerRepoMock.Setup(r => r.GetWithVehiclesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetCustomerWithVehiclesQuery(Guid.NewGuid()), CancellationToken.None));
    }
}

public class GetCustomersQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly GetCustomersQueryHandler _handler;

    public GetCustomersQueryHandlerTests()
    {
        _handler = new GetCustomersQueryHandler(_customerRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        var customers = new List<Customer>
        {
            new("John", "52998224725", "john@example.com", "11987654321", "hash")
        };
        _customerRepoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<Customer, bool>>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((customers, 1));

        var result = await _handler.Handle(new GetCustomersQuery(), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }
}
