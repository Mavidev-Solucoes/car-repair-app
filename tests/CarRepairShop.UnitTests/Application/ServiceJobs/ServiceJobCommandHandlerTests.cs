using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.ServiceJobs.Commands;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;
using Moq;

namespace CarRepairShop.UnitTests.Application.ServiceJobs;

public class ServiceJobCommandHandlerTests
{
    public class CreateServiceJobCommandHandlerTests
    {
        private readonly Mock<IServiceJobRepository> _jobRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly CreateServiceJobCommandHandler _handler;

        public CreateServiceJobCommandHandlerTests()
        {
            _handler = new CreateServiceJobCommandHandler(_jobRepo.Object, _uow.Object, _currentUser.Object);
        }

        [Fact]
        public async Task Handle_WhenNameAlreadyExists_ThrowsBusinessException()
        {
            _jobRepo.Setup(r => r.ExistsByNameAsync("Brake Repair", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var command = new CreateServiceJobCommand("Brake Repair", "Fix brakes", 3000m);

            await Assert.ThrowsAsync<BusinessException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidRequest_CreatesAndReturnsDto()
        {
            var userId = Guid.NewGuid();
            _currentUser.Setup(u => u.UserId).Returns(userId);
            _jobRepo.Setup(r => r.ExistsByNameAsync("Brake Repair", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _handler.Handle(new CreateServiceJobCommand("Brake Repair", "Fix brakes", 3000m), CancellationToken.None);

            Assert.Equal("Brake Repair", result.Name);
            Assert.Equal("Fix brakes", result.Description);
            Assert.Equal(3000m, result.Price);
            Assert.Equal(userId, result.CreatedUserId);
            _jobRepo.Verify(r => r.AddAsync(It.IsAny<ServiceJob>(), It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class UpdateServiceJobCommandHandlerTests
    {
        private readonly Mock<IServiceJobRepository> _jobRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly UpdateServiceJobCommandHandler _handler;

        public UpdateServiceJobCommandHandlerTests()
        {
            _handler = new UpdateServiceJobCommandHandler(_jobRepo.Object, _uow.Object, _currentUser.Object);
        }

        [Fact]
        public async Task Handle_WhenJobNotFound_ThrowsNotFoundException()
        {
            _jobRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceJob?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(new UpdateServiceJobCommand(Guid.NewGuid(), "Name", "Desc", 100m), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithValidRequest_UpdatesAndReturnsDto()
        {
            var userId = Guid.NewGuid();
            var job = new ServiceJob("Old Name", "Old Desc", 100m);
            _currentUser.Setup(u => u.UserId).Returns(userId);
            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            var result = await _handler.Handle(new UpdateServiceJobCommand(job.Id, "New Name", "New Desc", 999m), CancellationToken.None);

            Assert.Equal("New Name", result.Name);
            Assert.Equal("New Desc", result.Description);
            Assert.Equal(999m, result.Price);
            Assert.Equal(userId, result.LastUpdatedUserId);
            _jobRepo.Verify(r => r.Update(job), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class DeleteServiceJobCommandHandlerTests
    {
        private readonly Mock<IServiceJobRepository> _jobRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly DeleteServiceJobCommandHandler _handler;

        public DeleteServiceJobCommandHandlerTests()
        {
            _handler = new DeleteServiceJobCommandHandler(_jobRepo.Object, _uow.Object);
        }

        [Fact]
        public async Task Handle_WhenJobNotFound_ThrowsNotFoundException()
        {
            _jobRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceJob?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(new DeleteServiceJobCommand(Guid.NewGuid()), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenJobExists_DeletesAndReturnsUnit()
        {
            var job = new ServiceJob("Brake Repair", "Fix brakes", 3000m);
            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            var result = await _handler.Handle(new DeleteServiceJobCommand(job.Id), CancellationToken.None);

            Assert.Equal(Unit.Value, result);
            _jobRepo.Verify(r => r.Delete(job), Times.Once);
            _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
