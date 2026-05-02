using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.Customers.Commands;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;
using Moq;

namespace CarRepairShop.UnitTests.Customers;

public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _passwordHasherMock.Setup(x => x.Hash("Mudar@123")).Returns("hashed-default-password");
        _handler = new CreateCustomerCommandHandler(
            _repositoryMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesCustomerAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _repositoryMock
            .Setup(r => r.ExistsByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateCustomerCommand("John Doe", "52998224725", "john@example.com", "11987654321");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("John Doe", result.Name);
        Assert.Equal("52998224725", result.PersonalId);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("11987654321", result.Telephone);
        Assert.Equal(userId, result.CreatedUserId);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PersonalIdAlreadyExists_ThrowsBusinessException()
    {
        _repositoryMock
            .Setup(r => r.ExistsByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateCustomerCommand("John Doe", "52998224725", "john@example.com", "11987654321");

        await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PersonalIdWithFormatting_NormalizesBeforeCheck()
    {
        _repositoryMock
            .Setup(r => r.ExistsByDocumentAsync("52998224725", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateCustomerCommand("John Doe", "529.982.247-25", "john@example.com", "11987654321");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("52998224725", result.PersonalId);
        _repositoryMock.Verify(r => r.ExistsByDocumentAsync("52998224725", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullCurrentUser_CreatesCustomerWithNullCreatedUserId()
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns((Guid?)null);
        _repositoryMock
            .Setup(r => r.ExistsByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateCustomerCommand("John Doe", "52998224725", "john@example.com", "11987654321");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Null(result.CreatedUserId);
    }
}

public class UpdateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly UpdateCustomerCommandHandler _handler;

    public UpdateCustomerCommandHandlerTests()
    {
        _handler = new UpdateCustomerCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_UpdatesAndReturnsDto()
    {
        var customerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321", "hash");
        _currentUserServiceMock.Setup(s => s.UserId).Returns(userId);
        _repositoryMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var command = new UpdateCustomerCommand(customerId, "Jane Smith", "jane@example.com", "11912345678");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Jane Smith", result.Name);
        Assert.Equal("jane@example.com", result.Email);
        Assert.Equal("11912345678", result.Telephone);

        _repositoryMock.Verify(r => r.Update(customer), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var command = new UpdateCustomerCommand(customerId, "Jane Smith", "jane@example.com", "11912345678");

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));

        _repositoryMock.Verify(r => r.Update(It.IsAny<Customer>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class DeleteCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly DeleteCustomerCommandHandler _handler;

    public DeleteCustomerCommandHandlerTests()
    {
        _handler = new DeleteCustomerCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_DeletesAndReturnsUnit()
    {
        var customerId = Guid.NewGuid();
        var customer = new Customer("John Doe", "52998224725", "john@example.com", "11987654321", "hash");
        _repositoryMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _repositoryMock.Setup(r => r.HasServiceOrdersAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.HasVehiclesAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var command = new DeleteCustomerCommand(customerId);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        _repositoryMock.Verify(r => r.Delete(customer), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var command = new DeleteCustomerCommand(customerId);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));

        _repositoryMock.Verify(r => r.Delete(It.IsAny<Customer>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
