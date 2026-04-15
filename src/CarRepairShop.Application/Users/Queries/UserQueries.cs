using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.Users.Queries;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;

public record GetAllUsersQuery : IRequest<IEnumerable<UserDto>>;
