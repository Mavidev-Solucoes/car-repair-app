using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.OrderJobs.Queries;

public record GetOrderJobByIdQuery(Guid Id) : IRequest<ServiceOrderJobDto>;

public record GetOrderJobsQuery : IRequest<PagedResult<ServiceOrderJobDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool OrderDescending { get; init; }
    public string? Name { get; init; }
    public string? Status { get; init; }
    public Guid? AssignedUserId { get; init; }
}

public record GetOrderJobHistoryQuery(Guid OrderJobId) : IRequest<IEnumerable<ServiceOrderJobStatusHistoryDto>>;
