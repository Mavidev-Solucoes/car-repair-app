using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using MediatR;

namespace CarRepairShop.Application.Users.Queries;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken) as Employee
            ?? throw new NotFoundException(nameof(Employee), request.Id);

        return new UserDto(user.Id, user.Name, user.Email, user.Role, user.IsActive, user.CreatedAt);
    }
}

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, IEnumerable<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetAllUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users
            .OfType<Employee>()
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Role, u.IsActive, u.CreatedAt));
    }
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var query = users.OfType<Employee>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(user =>
                user.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                user.Email.Contains(request.Search, StringComparison.OrdinalIgnoreCase));
        }

        if (request.Role.HasValue)
            query = query.Where(user => user.Role == request.Role.Value);

        if (request.IsActive.HasValue)
            query = query.Where(user => user.IsActive == request.IsActive.Value);

        query = query.OrderBy(user => user.Name);

        var totalCount = query.Count();
        var items = query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(user => new UserDto(user.Id, user.Name, user.Email, user.Role, user.IsActive, user.CreatedAt))
            .ToList();

        return new PagedResult<UserDto>(items, totalCount, request.Page, request.PageSize);
    }
}
