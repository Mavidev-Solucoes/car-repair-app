using System.Linq.Expressions;
using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.ServiceJobs.Queries;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using Moq;

namespace CarRepairShop.UnitTests.Application.ServiceJobs;

public class ServiceJobQueryHandlerTests
{
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

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(new GetServiceJobByIdQuery(Guid.NewGuid()), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenJobFound_ReturnsMappedDto()
        {
            var job = new ServiceJob("Brake Repair", "Fix brakes", 3000m);
            _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);
            _jobRepo.Setup(r => r.GetAverageTimesInProgressAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<Guid, TimeSpan?> { [job.Id] = null });

            var result = await _handler.Handle(new GetServiceJobByIdQuery(job.Id), CancellationToken.None);

            Assert.Equal(job.Id, result.Id);
            Assert.Equal(job.Name, result.Name);
            Assert.Equal(job.Description, result.Description);
            Assert.Equal(job.Price, result.Price);
        }
    }

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
                new("Brake Repair", "Fix brakes", 3000m),
                new("Alignment", "Wheel alignment", 1500m)
            };

            _jobRepo.Setup(r => r.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<bool>(), It.IsAny<IEnumerable<Expression<Func<ServiceJob, bool>>>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((jobs, jobs.Count));
            _jobRepo.Setup(r => r.GetAverageTimesInProgressAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jobs.ToDictionary(j => j.Id, _ => (TimeSpan?)null));

            var result = await _handler.Handle(new GetServiceJobsQuery { Page = 1, PageSize = 10 }, CancellationToken.None);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task Handle_WithNameFilter_PassesFiltersToRepository()
        {
            _jobRepo.Setup(r => r.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<bool>(), It.IsAny<IEnumerable<Expression<Func<ServiceJob, bool>>>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<ServiceJob>(), 0));

            await _handler.Handle(new GetServiceJobsQuery { Name = "Brake", Page = 1, PageSize = 10 }, CancellationToken.None);

            _jobRepo.Verify(r => r.GetPagedAsync(
                1, 10, null, false,
                It.Is<IEnumerable<Expression<Func<ServiceJob, bool>>>>(filters => filters.Any()),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
