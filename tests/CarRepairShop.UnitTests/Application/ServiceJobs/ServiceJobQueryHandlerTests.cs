using CarRepairShop.Application.Common;
using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.ServiceJobs.Queries;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using Moq;

namespace CarRepairShop.UnitTests.Application.ServiceJobs;

public class ServiceJobQueryHandlerTests
{
    private static User CreateEmployee(string name = "Employee") =>
        new(name, $"{name.ToLower()}@shop.com", "hash", UserRole.Admin, UserType.Employee);

    private static ServiceJob CreateOpenJob(Guid? serviceOrderId = null) =>
        new(serviceOrderId ?? Guid.NewGuid(), "Brake Repair", "Fix brakes", 3000);

    // ── GetServiceJobByIdQueryHandler ────────────────────────────────────────

    public class GetServiceJobByIdQueryHandlerTests
    {
        private readonly Mock<IServiceJobRepository> _jobRepo = new();
        private readonly GetServiceJobByIdQueryHandler _handler;

        public GetServiceJobByIdQueryHandlerTests()
        {
            _handler = new GetServiceJobByIdQueryHandler(_jobRepo.Object);
        }

        [Fact]
        public async Task Handle_WhenJobNotFound_ThrowsNotFoundException()
        {
            _jobRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ServiceJob?)null);

            var query = new GetServiceJobByIdQuery(Guid.NewGuid());

            await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenJobFound_ReturnsMappedDto()
        {
            var job = CreateOpenJob();

            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            var query = new GetServiceJobByIdQuery(job.Id);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Equal(job.Id, result.Id);
            Assert.Equal(job.ServiceOrderId, result.ServiceOrderId);
            Assert.Equal(job.Name, result.Name);
            Assert.Equal(job.Description, result.Description);
            Assert.Equal(job.UnitCost, result.UnitCost);
            Assert.Equal("Open", result.Status);
            Assert.Null(result.AssignedUserId);
        }
    }

    // ── GetServiceJobsQueryHandler ───────────────────────────────────────────

    public class GetServiceJobsQueryHandlerTests
    {
        private readonly Mock<IServiceJobRepository> _jobRepo = new();
        private readonly GetServiceJobsQueryHandler _handler;

        public GetServiceJobsQueryHandlerTests()
        {
            _handler = new GetServiceJobsQueryHandler(_jobRepo.Object);
        }

        [Fact]
        public async Task Handle_WithNoFilters_ReturnsPagedResult()
        {
            var jobs = new List<ServiceJob>
            {
                CreateOpenJob(),
                CreateOpenJob()
            };

            _jobRepo.Setup(r => r.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<bool>(), It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceJob, bool>>>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((jobs, 2));

            var query = new GetServiceJobsQuery { Page = 1, PageSize = 10 };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
        }

        [Fact]
        public async Task Handle_WithNameFilter_PassesFiltersToRepository()
        {
            _jobRepo.Setup(r => r.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<bool>(), It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceJob, bool>>>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<ServiceJob>(), 0));

            var query = new GetServiceJobsQuery { Name = "Brake", Page = 1, PageSize = 10 };

            var result = await _handler.Handle(query, CancellationToken.None);

            _jobRepo.Verify(r => r.GetPagedAsync(
                1, 10, null, false,
                It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceJob, bool>>>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithStatusFilter_PassesFiltersToRepository()
        {
            _jobRepo.Setup(r => r.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<bool>(), It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceJob, bool>>>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<ServiceJob>(), 0));

            var query = new GetServiceJobsQuery { Status = "Open", Page = 1, PageSize = 10 };

            var result = await _handler.Handle(query, CancellationToken.None);

            _jobRepo.Verify(r => r.GetPagedAsync(
                1, 10, null, false,
                It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceJob, bool>>>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithAssignedUserIdFilter_PassesFiltersToRepository()
        {
            var assignedUserId = Guid.NewGuid();

            _jobRepo.Setup(r => r.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<bool>(), It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceJob, bool>>>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<ServiceJob>(), 0));

            var query = new GetServiceJobsQuery { AssignedUserId = assignedUserId, Page = 1, PageSize = 10 };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task Handle_WithInvalidStatusFilter_IgnoresStatusFilter()
        {
            _jobRepo.Setup(r => r.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<bool>(), It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceJob, bool>>>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<ServiceJob>(), 0));

            var query = new GetServiceJobsQuery { Status = "InvalidStatus", Page = 1, PageSize = 10 };

            // Should not throw - just ignores invalid status
            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task Handle_ReturnsDtosMappedCorrectly()
        {
            var job = CreateOpenJob();
            _jobRepo.Setup(r => r.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<bool>(), It.IsAny<IEnumerable<System.Linq.Expressions.Expression<Func<ServiceJob, bool>>>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<ServiceJob> { job }, 1));

            var query = new GetServiceJobsQuery { Page = 1, PageSize = 10 };

            var result = await _handler.Handle(query, CancellationToken.None);

            var dto = result.Items.Single();
            Assert.Equal(job.Id, dto.Id);
            Assert.Equal(job.Name, dto.Name);
            Assert.Equal("Open", dto.Status);
        }
    }

    // ── GetServiceJobHistoryQueryHandler ─────────────────────────────────────

    public class GetServiceJobHistoryQueryHandlerTests
    {
        private readonly Mock<IServiceJobRepository> _jobRepo = new();
        private readonly GetServiceJobHistoryQueryHandler _handler;

        public GetServiceJobHistoryQueryHandlerTests()
        {
            _handler = new GetServiceJobHistoryQueryHandler(_jobRepo.Object);
        }

        [Fact]
        public async Task Handle_WhenNoHistory_ReturnsEmptyResult()
        {
            var serviceJobId = Guid.NewGuid();
            _jobRepo.Setup(r => r.GetHistoryAsync(serviceJobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ServiceJobStatusHistory>());

            var query = new GetServiceJobHistoryQuery(serviceJobId);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.Empty(result);
        }

        [Fact]
        public async Task Handle_WithHistory_ReturnsMappedDtos()
        {
            var employee = CreateEmployee();
            var job = CreateOpenJob();
            job.Acknowledge(employee);

            // Use the actual status history from the job
            var history = job.StatusHistory.ToList();

            _jobRepo.Setup(r => r.GetHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(history);

            var query = new GetServiceJobHistoryQuery(job.Id);

            var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

            Assert.Equal(2, result.Count);

            // First entry (initial) has no previous time
            Assert.Null(result[0].TimeInPreviousStatus);
            Assert.Null(result[0].FromStatus);
            Assert.Equal("Open", result[0].ToStatus);

            // Second entry (transition) has previous time
            Assert.NotNull(result[1].TimeInPreviousStatus);
            Assert.Equal("Open", result[1].FromStatus);
            Assert.Equal("Acknowledged", result[1].ToStatus);
        }

        [Fact]
        public async Task Handle_WithSingleEntry_ReturnsNullTimeInPreviousStatus()
        {
            var job = CreateOpenJob();
            var history = job.StatusHistory.ToList(); // only initial

            _jobRepo.Setup(r => r.GetHistoryAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(history);

            var query = new GetServiceJobHistoryQuery(job.Id);

            var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

            Assert.Single(result);
            Assert.Null(result[0].TimeInPreviousStatus);
        }
    }
}
