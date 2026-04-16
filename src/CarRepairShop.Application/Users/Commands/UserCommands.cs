using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Enums;
using MediatR;

namespace CarRepairShop.Application.Users.Commands;

public record CreateUserCommand(string Name, string Email, string Password, UserRole Role) : IRequest<UserDto>;

public record UpdateUserCommand(Guid Id, string Name, string Email, UserRole Role) : IRequest<UserDto>;

public record ChangePasswordCommand(Guid Id, string CurrentPassword, string NewPassword) : IRequest<Unit>;

public record DeactivateUserCommand(Guid Id) : IRequest<Unit>;
