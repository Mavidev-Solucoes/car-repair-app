using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.ServiceJobs.Commands;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CarRepairShop.UnitTests.Application.ServiceJobs;

public class ServiceJobCommandHandlerTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateEmployee(string name = "Employee") =>
        new(name, $"{name.ToLower()}@shop.com", "hash", UserRole.Admin, UserType.Employee);

    private static ServiceJob CreateOpenJob(Guid? serviceOrderId = null) =>
        new(serviceOrderId ?? Guid.NewGuid(), "Brake Repair", "Fix brakes", 3000);

    private static ServiceJob CreateAcknowledgedJob(User employee, Guid? serviceOrderId = null)
    {
        var job = CreateOpenJob(serviceOrderId);
        job.Acknowledge(employee);
        return job;
    }

    private static ServiceJob CreateInProgressJob(User employee, Guid? serviceOrderId = null)
    {
        var job = CreateAcknowledgedJob(employee, serviceOrderId);
        job.StartProgress(employee.Id);
        return job;
    }

    private static ServiceOrder CreateServiceOrder(Guid assignedUserId, ServiceStatus status = ServiceStatus.Diagnosing)
    {
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), assignedUserId);

        // Set status directly via reflection to avoid complex business logic setup
        typeof(ServiceOrder)
            .GetProperty("Status", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .SetValue(order, status);

        return order;
    }

    private static void SetServiceOrderOnJob(ServiceJob job, ServiceOrder order)
    {
        var prop = typeof(ServiceJob).GetProperty("ServiceOrder",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!;
        prop.SetValue(job, order);
    }

    // ── UpdateServiceJobCommandHandler ───────────────────────────────────────

    public class UpdateServiceJobCommandHandlerTests
    {
        private readonly Mock<IServiceJobRepository> _jobRepo = new();
        private readonly Mock<IServiceOrderRepository> _orderRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly UpdateServiceJobCommandHandler _handler;

        public UpdateServiceJobCommandHandlerTests()
        {
            _handler = new UpdateServiceJobCommandHandler(
                _jobRepo.Object,
                _orderRepo.Object,
                _uow.Object,
                _currentUser.Object);
        }

        [Fact]
        public async Task Handle_WhenJobNotFound_ThrowsNotFoundException()
        {
            _jobRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceJob?)null);

            var command = new UpdateServiceJobCommand(Guid.NewGuid(), "Name", "Desc", 100);

            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenJobIsNotOpen_ThrowsBusinessException()
        {
            var employee = CreateEmployee();
            var job = CreateAcknowledgedJob(employee);

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            var command = new UpdateServiceJobCommand(job.Id, "Name", "Desc", 100);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("Open", ex.Message);
        }

        [Fact]
        public async Task Handle_WhenOrderNotFound_ThrowsNotFoundException()
        {
            var job = CreateOpenJob();

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceOrder?)null);

            var command = new UpdateServiceJobCommand(job.Id, "Name", "Desc", 100);

            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenOrderIsNotDiagnosing_ThrowsBusinessException()
        {
            var userId = Guid.NewGuid();
            var job = CreateOpenJob();

            var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId);
            // order is in Received status (not Diagnosing)

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var command = new UpdateServiceJobCommand(job.Id, "Name", "Desc", 100);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("Diagnosing", ex.Message);
        }

        [Fact]
        public async Task Handle_WhenUserNotAuthenticated_ThrowsBusinessException()
        {
            var userId = Guid.NewGuid();
            var job = CreateOpenJob();
            var order = CreateServiceOrder(userId);

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _currentUser.Setup(u => u.UserId).Returns((Guid?)null);

            var command = new UpdateServiceJobCommand(job.Id, "Name", "Desc", 100);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("authenticated", ex.Message);
        }

        [Fact]
        public async Task Handle_WhenUserIsNotAssignedEmployee_ThrowsBusinessException()
        {
            var assignedUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var job = CreateOpenJob();
            var order = CreateServiceOrder(assignedUserId);

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _currentUser.Setup(u => u.UserId).Returns(otherUserId);

            var command = new UpdateServiceJobCommand(job.Id, "Name", "Desc", 100);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("assigned employee", ex.Message);
        }

        [Fact]
        public async Task Handle_WithValidRequest_UpdatesJobAndReturnsDto()
        {
            var assignedUserId = Guid.NewGuid();
            var job = CreateOpenJob();
            var order = CreateServiceOrder(assignedUserId);

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _currentUser.Setup(u => u.UserId).Returns(assignedUserId);
            _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = new UpdateServiceJobCommand(job.Id, "Updated Name", "Updated Desc", 9999);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal("Updated Name", result.Name);
            Assert.Equal("Updated Desc", result.Description);
            Assert.Equal(9999, result.UnitCost);
            _jobRepo.Verify(r => r.Update(job), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    // ── DeleteServiceJobCommandHandler ───────────────────────────────────────

    public class DeleteServiceJobCommandHandlerTests
    {
        private readonly Mock<IServiceJobRepository> _jobRepo = new();
        private readonly Mock<IServiceOrderRepository> _orderRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly DeleteServiceJobCommandHandler _handler;

        public DeleteServiceJobCommandHandlerTests()
        {
            _handler = new DeleteServiceJobCommandHandler(
                _jobRepo.Object,
                _orderRepo.Object,
                _uow.Object,
                _currentUser.Object);
        }

        [Fact]
        public async Task Handle_WhenJobNotFound_ThrowsNotFoundException()
        {
            _jobRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceJob?)null);

            var command = new DeleteServiceJobCommand(Guid.NewGuid());

            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenJobIsNotOpen_ThrowsBusinessException()
        {
            var employee = CreateEmployee();
            var job = CreateAcknowledgedJob(employee);

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            var command = new DeleteServiceJobCommand(job.Id);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("Open", ex.Message);
        }

        [Fact]
        public async Task Handle_WhenOrderNotFound_ThrowsNotFoundException()
        {
            var job = CreateOpenJob();

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceOrder?)null);

            var command = new DeleteServiceJobCommand(job.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenOrderIsNotDiagnosing_ThrowsBusinessException()
        {
            var userId = Guid.NewGuid();
            var job = CreateOpenJob();
            var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), userId); // Received status

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var command = new DeleteServiceJobCommand(job.Id);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("Diagnosing", ex.Message);
        }

        [Fact]
        public async Task Handle_WhenUserNotAuthenticated_ThrowsBusinessException()
        {
            var userId = Guid.NewGuid();
            var job = CreateOpenJob();
            var order = CreateServiceOrder(userId);

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _currentUser.Setup(u => u.UserId).Returns((Guid?)null);

            var command = new DeleteServiceJobCommand(job.Id);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("authenticated", ex.Message);
        }

        [Fact]
        public async Task Handle_WhenUserIsNotAssignedEmployee_ThrowsBusinessException()
        {
            var assignedUserId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var job = CreateOpenJob();
            var order = CreateServiceOrder(assignedUserId);

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _currentUser.Setup(u => u.UserId).Returns(otherUserId);

            var command = new DeleteServiceJobCommand(job.Id);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("assigned employee", ex.Message);
        }

        [Fact]
        public async Task Handle_WithValidRequest_DeletesJobAndReturnsUnit()
        {
            var assignedUserId = Guid.NewGuid();
            var job = CreateOpenJob();
            var order = CreateServiceOrder(assignedUserId);

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _currentUser.Setup(u => u.UserId).Returns(assignedUserId);
            _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = new DeleteServiceJobCommand(job.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(MediatR.Unit.Value, result);
            _jobRepo.Verify(r => r.Delete(job), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    // ── AcknowledgeJobCommandHandler ─────────────────────────────────────────

    public class AcknowledgeJobCommandHandlerTests
    {
        private readonly Mock<IServiceJobRepository> _jobRepo = new();
        private readonly Mock<IServiceOrderRepository> _orderRepo = new();
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly AcknowledgeJobCommandHandler _handler;

        public AcknowledgeJobCommandHandlerTests()
        {
            _handler = new AcknowledgeJobCommandHandler(
                _jobRepo.Object,
                _orderRepo.Object,
                _userRepo.Object,
                _uow.Object,
                _currentUser.Object);
        }

        [Fact]
        public async Task Handle_WhenJobNotFound_ThrowsNotFoundException()
        {
            _jobRepo.Setup(r => r.GetByIdWithHistoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceJob?)null);

            var command = new AcknowledgeJobCommand(Guid.NewGuid());

            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenOrderNotFound_ThrowsNotFoundException()
        {
            var job = CreateOpenJob();

            _jobRepo.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceOrder?)null);

            var command = new AcknowledgeJobCommand(job.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenOrderIsNotDiagnosing_ThrowsBusinessException()
        {
            var assignedUserId = Guid.NewGuid();
            var job = CreateOpenJob();
            var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), assignedUserId); // Received

            _jobRepo.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var command = new AcknowledgeJobCommand(job.Id);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("Diagnosing", ex.Message);
        }

        [Fact]
        public async Task Handle_WhenUserNotAuthenticated_ThrowsBusinessException()
        {
            var assignedUserId = Guid.NewGuid();
            var job = CreateOpenJob();
            var order = CreateServiceOrder(assignedUserId);

            _jobRepo.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _currentUser.Setup(u => u.UserId).Returns((Guid?)null);

            var command = new AcknowledgeJobCommand(job.Id);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("authenticated", ex.Message);
        }

        [Fact]
        public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
        {
            var assignedUserId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var job = CreateOpenJob();
            var order = CreateServiceOrder(assignedUserId);

            _jobRepo.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _currentUser.Setup(u => u.UserId).Returns(userId);
            _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var command = new AcknowledgeJobCommand(job.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidRequest_AcknowledgesJobAndReturnsDto()
        {
            var assignedUserId = Guid.NewGuid();
            var job = CreateOpenJob();
            var order = CreateServiceOrder(assignedUserId);
            var employee = CreateEmployee();

            _jobRepo.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _currentUser.Setup(u => u.UserId).Returns(employee.Id);
            _userRepo.Setup(r => r.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(employee);
            _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = new AcknowledgeJobCommand(job.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal("Acknowledged", result.Status);
            Assert.Equal(employee.Id, result.AssignedUserId);
            _jobRepo.Verify(r => r.Update(job), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    // ── StartJobProgressCommandHandler ───────────────────────────────────────

    public class StartJobProgressCommandHandlerTests
    {
        private readonly Mock<IServiceJobRepository> _jobRepo = new();
        private readonly Mock<IServiceOrderRepository> _orderRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly StartJobProgressCommandHandler _handler;

        public StartJobProgressCommandHandlerTests()
        {
            _handler = new StartJobProgressCommandHandler(
                _jobRepo.Object,
                _orderRepo.Object,
                _uow.Object,
                _currentUser.Object);
        }

        [Fact]
        public async Task Handle_WhenJobNotFound_ThrowsNotFoundException()
        {
            _jobRepo.Setup(r => r.GetByIdWithHistoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceJob?)null);

            var command = new StartJobProgressCommand(Guid.NewGuid());

            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenOrderNotFound_ThrowsNotFoundException()
        {
            var employee = CreateEmployee();
            var job = CreateAcknowledgedJob(employee);

            _jobRepo.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceOrder?)null);

            var command = new StartJobProgressCommand(job.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenOrderIsNotExecuting_ThrowsBusinessException()
        {
            var assignedUserId = Guid.NewGuid();
            var employee = CreateEmployee();
            var job = CreateAcknowledgedJob(employee);
            var order = CreateServiceOrder(assignedUserId); // Diagnosing, not Executing

            _jobRepo.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var command = new StartJobProgressCommand(job.Id);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("approved", ex.Message);
        }

        [Fact]
        public async Task Handle_WhenUserNotAuthenticated_ThrowsBusinessException()
        {
            var assignedUserId = Guid.NewGuid();
            var employee = CreateEmployee();
            var job = CreateAcknowledgedJob(employee);
            var order = CreateServiceOrder(assignedUserId, ServiceStatus.Executing);

            _jobRepo.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _currentUser.Setup(u => u.UserId).Returns((Guid?)null);

            var command = new StartJobProgressCommand(job.Id);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("authenticated", ex.Message);
        }

        [Fact]
        public async Task Handle_WithValidRequest_StartsProgressAndReturnsDto()
        {
            var assignedUserId = Guid.NewGuid();
            var employee = CreateEmployee();
            var job = CreateAcknowledgedJob(employee);
            var order = CreateServiceOrder(assignedUserId, ServiceStatus.Executing);

            _jobRepo.Setup(r => r.GetByIdWithHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _orderRepo.Setup(r => r.GetByIdAsync(job.ServiceOrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            _currentUser.Setup(u => u.UserId).Returns(employee.Id);
            _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = new StartJobProgressCommand(job.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal("InProgress", result.Status);
            _jobRepo.Verify(r => r.Update(job), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    // ── CompleteJobCommandHandler ────────────────────────────────────────────

    public class CompleteJobCommandHandlerTests
    {
        private readonly Mock<IServiceJobRepository> _jobRepo = new();
        private readonly Mock<IServiceOrderRepository> _orderRepo = new();
        private readonly Mock<ICustomerRepository> _customerRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<IEmailService> _emailService = new();
        private readonly Mock<IEmailTemplateService> _emailTemplateService = new();
        private readonly Mock<ILogger<CompleteJobCommandHandler>> _logger = new();
        private readonly CompleteJobCommandHandler _handler;

        public CompleteJobCommandHandlerTests()
        {
            _handler = new CompleteJobCommandHandler(
                _jobRepo.Object,
                _orderRepo.Object,
                _customerRepo.Object,
                _uow.Object,
                _currentUser.Object,
                _emailService.Object,
                _emailTemplateService.Object,
                _logger.Object);
        }

        private ServiceJob CreateInProgressJobWithOrder(User employee, ServiceOrder order)
        {
            var job = new ServiceJob(order.Id, "Repair", "Fix it", 5000);
            job.Acknowledge(employee);
            job.StartProgress(employee.Id);
            SetServiceOrderOnJob(job, order);
            return job;
        }

        [Fact]
        public async Task Handle_WhenJobNotFound_ThrowsNotFoundException()
        {
            _jobRepo.Setup(r => r.GetByIdWithServiceOrderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceJob?)null);

            var command = new CompleteJobCommand(Guid.NewGuid());

            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenOrderIsNotExecuting_ThrowsBusinessException()
        {
            var employee = CreateEmployee();
            var assignedUserId = Guid.NewGuid();
            var order = CreateServiceOrder(assignedUserId); // Diagnosing, not Executing
            var job = CreateInProgressJobWithOrder(employee, order);

            _jobRepo.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            var command = new CompleteJobCommand(job.Id);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("approved", ex.Message);
        }

        [Fact]
        public async Task Handle_WhenUserNotAuthenticated_ThrowsBusinessException()
        {
            var employee = CreateEmployee();
            var assignedUserId = Guid.NewGuid();
            var order = CreateServiceOrder(assignedUserId, ServiceStatus.Executing);
            var job = CreateInProgressJobWithOrder(employee, order);

            _jobRepo.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _currentUser.Setup(u => u.UserId).Returns((Guid?)null);

            var command = new CompleteJobCommand(job.Id);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.Contains("authenticated", ex.Message);
        }

        [Fact]
        public async Task Handle_WhenCompleted_WithoutFinishingOrder_ReturnsDto()
        {
            var employee = CreateEmployee();
            var assignedUserId = Guid.NewGuid();
            var order = CreateServiceOrder(assignedUserId, ServiceStatus.Executing);
            var job = CreateInProgressJobWithOrder(employee, order);

            _jobRepo.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _currentUser.Setup(u => u.UserId).Returns(employee.Id);
            _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var command = new CompleteJobCommand(job.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal("Completed", result.Status);
            _jobRepo.Verify(r => r.Update(job), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            // Email not sent when order did not finish
            _emailService.Verify(
                s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenCompleted_AndOrderFinishes_SendsEmailToCustomer()
        {
            var employee = CreateEmployee();
            var assignedUserId = Guid.NewGuid();

            // Create order in Executing status with one job that we will complete
            var order = CreateServiceOrder(assignedUserId, ServiceStatus.Executing);
            var job = new ServiceJob(order.Id, "Repair", "Fix it", 5000);
            job.Acknowledge(employee);
            job.StartProgress(employee.Id);

            // Attach job to order so TryFinish finds all jobs complete
            var jobsField = typeof(ServiceOrder)
                .GetField("_serviceJobs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            ((List<ServiceJob>)jobsField.GetValue(order)!).Add(job);
            SetServiceOrderOnJob(job, order);

            var customer = new Customer("John", "12345678901", "john@email.com", "11999999999");

            _jobRepo.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _currentUser.Setup(u => u.UserId).Returns(employee.Id);
            _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _customerRepo.Setup(r => r.GetByIdAsync(order.CustomerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            _emailTemplateService.Setup(t => t.RenderServiceFinishedAsync(order, customer))
                .ReturnsAsync("<html>finished</html>");
            _emailService.Setup(s => s.SendAsync(
                    customer.Email, customer.Name, It.IsAny<string>(),
                    It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var command = new CompleteJobCommand(job.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal("Completed", result.Status);
            _emailService.Verify(
                s => s.SendAsync(customer.Email, customer.Name, It.IsAny<string>(),
                    It.IsAny<string>(), true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenOrderFinishes_ButCustomerNotFound_LogsWarning()
        {
            var employee = CreateEmployee();
            var assignedUserId = Guid.NewGuid();

            var order = CreateServiceOrder(assignedUserId, ServiceStatus.Executing);
            var job = new ServiceJob(order.Id, "Repair", "Fix it", 5000);
            job.Acknowledge(employee);
            job.StartProgress(employee.Id);

            var jobsField = typeof(ServiceOrder)
                .GetField("_serviceJobs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            ((List<ServiceJob>)jobsField.GetValue(order)!).Add(job);
            SetServiceOrderOnJob(job, order);

            _jobRepo.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _currentUser.Setup(u => u.UserId).Returns(employee.Id);
            _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _customerRepo.Setup(r => r.GetByIdAsync(order.CustomerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            var command = new CompleteJobCommand(job.Id);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal("Completed", result.Status);
            // Email should not be sent if customer not found
            _emailService.Verify(
                s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenEmailFails_DoesNotThrowAndReturnsDto()
        {
            var employee = CreateEmployee();
            var assignedUserId = Guid.NewGuid();

            var order = CreateServiceOrder(assignedUserId, ServiceStatus.Executing);
            var job = new ServiceJob(order.Id, "Repair", "Fix it", 5000);
            job.Acknowledge(employee);
            job.StartProgress(employee.Id);

            var jobsField = typeof(ServiceOrder)
                .GetField("_serviceJobs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            ((List<ServiceJob>)jobsField.GetValue(order)!).Add(job);
            SetServiceOrderOnJob(job, order);

            var customer = new Customer("John", "12345678901", "john@email.com", "11999999999");

            _jobRepo.Setup(r => r.GetByIdWithServiceOrderAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _currentUser.Setup(u => u.UserId).Returns(employee.Id);
            _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _customerRepo.Setup(r => r.GetByIdAsync(order.CustomerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
            _emailTemplateService.Setup(t => t.RenderServiceFinishedAsync(order, customer))
                .ThrowsAsync(new Exception("Email template error"));

            var command = new CompleteJobCommand(job.Id);

            // Should not throw even if email fails
            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal("Completed", result.Status);
        }
    }
}
