using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Enums;
using MediatR;

namespace CarRepairShop.Application.Users.Queries;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;

public record GetAllUsersQuery : IRequest<IEnumerable<UserDto>>;

public record GetUsersQuery : IRequest<PagedResult<UserDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Search { get; init; }
    public UserRole? Role { get; init; }
    public bool? IsActive { get; init; }
}
