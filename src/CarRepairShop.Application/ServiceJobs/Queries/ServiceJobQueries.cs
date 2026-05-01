using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.ServiceJobs.Queries;

public record GetServiceJobByIdQuery(Guid Id) : IRequest<ServiceJobDto>;

public record GetServiceJobsQuery : IRequest<PagedResult<ServiceJobDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool OrderDescending { get; init; } = false;

    // Filters
    public string? Name { get; init; }
    public string? Status { get; init; }
    public Guid? AssignedUserId { get; init; }
}

public record GetServiceJobHistoryQuery(Guid ServiceJobId) : IRequest<IEnumerable<ServiceJobStatusHistoryDto>>;
