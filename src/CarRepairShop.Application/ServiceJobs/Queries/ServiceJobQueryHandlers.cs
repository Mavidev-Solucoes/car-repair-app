using System.Linq.Expressions;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.ServiceJobs.Commands;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using MediatR;

namespace CarRepairShop.Application.ServiceJobs.Queries;

public class GetServiceJobByIdQueryHandler : IRequestHandler<GetServiceJobByIdQuery, ServiceJobDto>
{
    private readonly IServiceJobRepository _serviceJobRepository;

    public GetServiceJobByIdQueryHandler(IServiceJobRepository serviceJobRepository)
    {
        _serviceJobRepository = serviceJobRepository;
    }

    public async Task<ServiceJobDto> Handle(GetServiceJobByIdQuery request, CancellationToken cancellationToken)
    {
        var serviceJob = await _serviceJobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceJob), request.Id);

        return ServiceJobMapper.MapToDto(serviceJob);
    }
}

public class GetServiceJobsQueryHandler : IRequestHandler<GetServiceJobsQuery, PagedResult<ServiceJobDto>>
{
    private readonly IServiceJobRepository _serviceJobRepository;

    public GetServiceJobsQueryHandler(IServiceJobRepository serviceJobRepository)
    {
        _serviceJobRepository = serviceJobRepository;
    }

    public async Task<PagedResult<ServiceJobDto>> Handle(GetServiceJobsQuery request, CancellationToken cancellationToken)
    {
        var filters = BuildFilters(request);

        var (items, totalCount) = await _serviceJobRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.OrderBy,
            request.OrderDescending,
            filters,
            cancellationToken);

        return new PagedResult<ServiceJobDto>(
            items.Select(ServiceJobMapper.MapToDto),
            totalCount,
            request.Page,
            request.PageSize);
    }

    private static IEnumerable<Expression<Func<ServiceJob, bool>>> BuildFilters(GetServiceJobsQuery request)
    {
        var filters = new List<Expression<Func<ServiceJob, bool>>>();

        if (!string.IsNullOrWhiteSpace(request.Name))
            filters.Add(sj => sj.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<JobStatus>(request.Status, true, out var status))
            filters.Add(sj => sj.Status == status);

        if (request.AssignedUserId.HasValue)
            filters.Add(sj => sj.AssignedUserId == request.AssignedUserId);

        return filters;
    }
}

public class GetServiceJobHistoryQueryHandler : IRequestHandler<GetServiceJobHistoryQuery, IEnumerable<ServiceJobStatusHistoryDto>>
{
    private readonly IServiceJobRepository _serviceJobRepository;

    public GetServiceJobHistoryQueryHandler(IServiceJobRepository serviceJobRepository)
    {
        _serviceJobRepository = serviceJobRepository;
    }

    public async Task<IEnumerable<ServiceJobStatusHistoryDto>> Handle(GetServiceJobHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = (await _serviceJobRepository.GetHistoryAsync(request.ServiceJobId, cancellationToken))
            .ToList();

        return history.Select((entry, index) =>
        {
            TimeSpan? timeInPreviousStatus = null;
            if (index > 0)
                timeInPreviousStatus = entry.ChangedAt - history[index - 1].ChangedAt;

            return new ServiceJobStatusHistoryDto(
                entry.Id,
                entry.ServiceJobId,
                entry.FromStatus?.ToString(),
                entry.ToStatus.ToString(),
                entry.ChangedAt,
                entry.ChangedByUserId,
                timeInPreviousStatus);
        });
    }
}
