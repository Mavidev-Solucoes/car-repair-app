using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.ServiceOrders.Commands;
using CarRepairShop.Application.ServiceOrders.Queries;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using Moq;

namespace CarRepairShop.UnitTests.Application.ServiceOrders;

public class GetServiceOrderByIdQueryHandlerTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly GetServiceOrderByIdQueryHandler _handler;

    public GetServiceOrderByIdQueryHandlerTests()
    {
        _handler = new GetServiceOrderByIdQueryHandler(
            _orderRepoMock.Object,
            _userRepoMock.Object,
            _currentUserMock.Object);
    }

    private static ServiceOrder CreateOrder()
    {
        var vehicleId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        return new ServiceOrder(vehicleId, customerId, userId);
    }

    [Fact]
    public async Task Handle_OrderExists_ReturnsDto()
    {
        var order = CreateOrder();
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        var result = await _handler.Handle(new GetServiceOrderByIdQuery(order.Id), CancellationToken.None);

        Assert.Equal(order.Id, result.Id);
        Assert.Equal("Received", result.Status);
    }

    [Fact]
    public async Task Handle_OrderWithItemsAndJobs_MapsNestedCollections()
    {
        var order = CreateOrder();
        var employeeId = order.AssignedUserId;
        var item = new ServiceOrderItem(order.Id, Guid.NewGuid(), "Oil filter", 50m, 2);
        order.AddServiceItem(item, employeeId);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Brake Check", "Check brakes", 200m);
        order.AttachServiceJob(job, employeeId);

        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        var result = await _handler.Handle(new GetServiceOrderByIdQuery(order.Id), CancellationToken.None);

        var resultItems = result.ServiceItems.ToList();
        Assert.Single(resultItems);
        Assert.Equal(item.Id, resultItems[0].Id);
        Assert.Equal("Oil filter", resultItems[0].Description);
        Assert.Equal(50m, resultItems[0].Price);
        Assert.Equal(2, resultItems[0].Quantity);

        var resultJobs = result.ServiceJobs.ToList();
        Assert.Single(resultJobs);
        Assert.Equal(job.Id, resultJobs[0].Id);
        Assert.Equal("Brake Check", resultJobs[0].Name);

        var resultHistory = result.StatusHistory.ToList();
        Assert.NotEmpty(resultHistory);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetServiceOrderByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CustomerTryingToAccessOtherOrder_ThrowsBusinessException()
    {
        var order = CreateOrder();
        var customerId = Guid.NewGuid();
        var customer = new Customer("Customer", "52998224725", "c@c.com", "11987654321", "hash");
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _currentUserMock.Setup(s => s.UserId).Returns(customerId);
        _userRepoMock.Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _handler.Handle(new GetServiceOrderByIdQuery(order.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmployeeAccessesAnyOrder_ReturnsDto()
    {
        var order = CreateOrder();
        var employee = new Employee("Admin", "admin@shop.com", "hash", CarRepairShop.Domain.Enums.UserRole.Admin);
        _orderRepoMock.Setup(r => r.GetWithAllDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _currentUserMock.Setup(s => s.UserId).Returns(employee.Id);
        _userRepoMock.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var result = await _handler.Handle(new GetServiceOrderByIdQuery(order.Id), CancellationToken.None);

        Assert.Equal(order.Id, result.Id);
    }
}

public class GetAllServiceOrdersQueryHandlerTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly GetAllServiceOrdersQueryHandler _handler;

    public GetAllServiceOrdersQueryHandlerTests()
    {
        _handler = new GetAllServiceOrdersQueryHandler(
            _orderRepoMock.Object,
            _userRepoMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_NoCurrentUser_ReturnsAllOrders()
    {
        var orders = new List<ServiceOrder>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())
        };
        _orderRepoMock.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        var result = await _handler.Handle(new GetAllServiceOrdersQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task Handle_CustomerRole_ReturnsOnlyOwnOrders()
    {
        var customerId = Guid.NewGuid();
        var orders = new List<ServiceOrder>
        {
            new(Guid.NewGuid(), customerId, Guid.NewGuid()),
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())
        };
        var customer = new Customer("Cust", "52998224725", "c@c.com", "11987654321", "hash");
        _orderRepoMock.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);
        _currentUserMock.Setup(s => s.UserId).Returns(customerId);
        _userRepoMock.Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _handler.Handle(new GetAllServiceOrdersQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.All(result, o => Assert.Equal(customerId, o.CustomerId));
    }

    [Fact]
    public async Task Handle_AlwaysExcludesFinishedAndDelivered()
    {
        var employeeId = Guid.NewGuid();
        var receivedOrder  = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employeeId);
        var finishedOrder  = CreateOrderWithStatus(ServiceStatus.Finished, employeeId);
        var deliveredOrder = CreateOrderWithStatus(ServiceStatus.Delivered, employeeId);

        _orderRepoMock.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceOrder> { receivedOrder, finishedOrder, deliveredOrder });
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        var result = (await _handler.Handle(new GetAllServiceOrdersQuery(), CancellationToken.None)).ToList();

        Assert.Single(result);
        Assert.Equal("Received", result[0].Status);
    }

    [Fact]
    public async Task Handle_OrdersByPriority_ExecutingFirst()
    {
        var employeeId = Guid.NewGuid();
        var receivedOrder          = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employeeId);
        var diagnosingOrder        = CreateOrderWithStatus(ServiceStatus.Diagnosing, employeeId);
        var waitingForApprovalOrder = CreateOrderWithStatus(ServiceStatus.WaitingForApproval, employeeId);
        var executingOrder         = CreateOrderWithStatus(ServiceStatus.Executing, employeeId);

        _orderRepoMock.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceOrder> { receivedOrder, diagnosingOrder, waitingForApprovalOrder, executingOrder });
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        var result = (await _handler.Handle(new GetAllServiceOrdersQuery(), CancellationToken.None)).ToList();

        Assert.Equal(4, result.Count);
        Assert.Equal("Executing",          result[0].Status);
        Assert.Equal("WaitingForApproval", result[1].Status);
        Assert.Equal("Diagnosing",         result[2].Status);
        Assert.Equal("Received",           result[3].Status);
    }

    [Fact]
    public async Task Handle_SameStatus_OldestCreatedAtFirst()
    {
        var employeeId = Guid.NewGuid();
        var olderOrder = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employeeId);
        var newerOrder = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employeeId);

        // Manipulate CreatedAt so olderOrder was created before newerOrder.
        var createdAtProp = typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        createdAtProp?.SetValue(olderOrder, DateTime.UtcNow.AddHours(-2));
        createdAtProp?.SetValue(newerOrder, DateTime.UtcNow.AddHours(-1));

        _orderRepoMock.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceOrder> { newerOrder, olderOrder });
        _currentUserMock.Setup(s => s.UserId).Returns((Guid?)null);

        var result = (await _handler.Handle(new GetAllServiceOrdersQuery(), CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(olderOrder.Id, result[0].Id);
        Assert.Equal(newerOrder.Id, result[1].Id);
    }

    private static ServiceOrder CreateOrderWithStatus(ServiceStatus status, Guid employeeId)
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employeeId);
        var statusProp = typeof(ServiceOrder).GetProperty(nameof(ServiceOrder.Status),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        statusProp?.SetValue(order, status);
        return order;
    }
}

public class GetServiceStatusHistoryCommandHandlerTests
{
    private readonly Mock<IServiceOrderRepository> _orderRepoMock = new();
    private readonly GetServiceStatusHistoryCommandHandler _handler;

    public GetServiceStatusHistoryCommandHandlerTests()
    {
        _handler = new GetServiceStatusHistoryCommandHandler(_orderRepoMock.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetServiceStatusHistoryCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderExists_ReturnsHistory()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var history = new List<ServiceStatusHistory>();
        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetStatusHistoryAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var result = await _handler.Handle(new GetServiceStatusHistoryCommand(order.Id), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_OrderWithHistory_ReturnsMappedDtos()
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        // order.StatusHistory has one initial entry (FromStatus=null, ToStatus=Received)
        var history = order.StatusHistory.ToList();
        _orderRepoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.GetStatusHistoryAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var result = (await _handler.Handle(new GetServiceStatusHistoryCommand(order.Id), CancellationToken.None)).ToList();

        Assert.Single(result);
        Assert.Null(result[0].FromStatus);
        Assert.Equal("Received", result[0].ToStatus);
    }
}
