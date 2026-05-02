using System.Linq.Expressions;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Application.OrderJobs.Commands;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Repositories;
using MediatR;

namespace CarRepairShop.Application.OrderJobs.Queries;

public class GetOrderJobByIdQueryHandler : IRequestHandler<GetOrderJobByIdQuery, ServiceOrderJobDto>
{
    private readonly IServiceOrderJobRepository _serviceOrderJobRepository;

    public GetOrderJobByIdQueryHandler(IServiceOrderJobRepository serviceOrderJobRepository)
    {
        _serviceOrderJobRepository = serviceOrderJobRepository;
    }

    public async Task<ServiceOrderJobDto> Handle(GetOrderJobByIdQuery request, CancellationToken cancellationToken)
    {
        var job = await _serviceOrderJobRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrderJob), request.Id);

        return OrderJobMapper.MapToDto(job);
    }
}

public class GetOrderJobsQueryHandler : IRequestHandler<GetOrderJobsQuery, PagedResult<ServiceOrderJobDto>>
{
    private readonly IServiceOrderJobRepository _serviceOrderJobRepository;

    public GetOrderJobsQueryHandler(IServiceOrderJobRepository serviceOrderJobRepository)
    {
        _serviceOrderJobRepository = serviceOrderJobRepository;
    }

    public async Task<PagedResult<ServiceOrderJobDto>> Handle(GetOrderJobsQuery request, CancellationToken cancellationToken)
    {
        var filters = BuildFilters(request);
        var (items, totalCount) = await _serviceOrderJobRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.OrderBy,
            request.OrderDescending,
            filters,
            cancellationToken);

        return new PagedResult<ServiceOrderJobDto>(
            items.Select(OrderJobMapper.MapToDto),
            totalCount,
            request.Page,
            request.PageSize);
    }

    private static IEnumerable<Expression<Func<ServiceOrderJob, bool>>> BuildFilters(GetOrderJobsQuery request)
    {
        var filters = new List<Expression<Func<ServiceOrderJob, bool>>>();

        if (!string.IsNullOrWhiteSpace(request.Name))
            filters.Add(job => job.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<JobStatus>(request.Status, true, out var status))
            filters.Add(job => job.Status == status);

        if (request.AssignedUserId.HasValue)
            filters.Add(job => job.AssignedUserId == request.AssignedUserId);

        return filters;
    }
}

public class GetOrderJobHistoryQueryHandler : IRequestHandler<GetOrderJobHistoryQuery, IEnumerable<ServiceOrderJobStatusHistoryDto>>
{
    private readonly IServiceOrderJobRepository _serviceOrderJobRepository;

    public GetOrderJobHistoryQueryHandler(IServiceOrderJobRepository serviceOrderJobRepository)
    {
        _serviceOrderJobRepository = serviceOrderJobRepository;
    }

    public async Task<IEnumerable<ServiceOrderJobStatusHistoryDto>> Handle(GetOrderJobHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = (await _serviceOrderJobRepository.GetHistoryAsync(request.OrderJobId, cancellationToken)).ToList();

        return history.Select((entry, index) =>
        {
            TimeSpan? timeInPreviousStatus = null;
            if (index > 0)
                timeInPreviousStatus = entry.ChangedAt - history[index - 1].ChangedAt;

            return new ServiceOrderJobStatusHistoryDto(
                entry.Id,
                entry.ServiceOrderJobId,
                entry.FromStatus?.ToString(),
                entry.ToStatus.ToString(),
                entry.ChangedAt,
                entry.ChangedByUserId,
                timeInPreviousStatus);
        });
    }
}
