using System.Linq.Expressions;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using MediatR;

namespace CarRepairShop.Application.Customers.Queries;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerByIdQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        return CustomerQueryMapper.MapToDto(customer);
    }
}

public class GetCustomerWithVehiclesQueryHandler : IRequestHandler<GetCustomerWithVehiclesQuery, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerWithVehiclesQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto> Handle(GetCustomerWithVehiclesQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetWithVehiclesAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        return CustomerQueryMapper.MapToDto(customer);
    }
}

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomersQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var filters = BuildFilters(request);

        var (items, totalCount) = await _customerRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.OrderBy,
            request.OrderDescending,
            filters,
            cancellationToken);

        return new PagedResult<CustomerDto>(
            items.Select(CustomerQueryMapper.MapToDto),
            totalCount,
            request.Page,
            request.PageSize);
    }

    private static IEnumerable<Expression<Func<Customer, bool>>> BuildFilters(GetCustomersQuery request)
    {
        var filters = new List<Expression<Func<Customer, bool>>>();

        if (!string.IsNullOrWhiteSpace(request.Name))
            filters.Add(c => c.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Email))
            filters.Add(c => c.Email.Contains(request.Email));

        if (!string.IsNullOrWhiteSpace(request.PersonalId))
        {
            var digits = new string(request.PersonalId.Where(char.IsDigit).ToArray());
            filters.Add(c => c.PersonalId.Contains(digits));
        }

        if (!string.IsNullOrWhiteSpace(request.Telephone))
        {
            var digits = new string(request.Telephone.Where(char.IsDigit).ToArray());
            filters.Add(c => c.Telephone.Contains(digits));
        }

        return filters;
    }
}

file static class CustomerQueryMapper
{
    internal static CustomerDto MapToDto(Customer customer) =>
        new(customer.Id, customer.Name, customer.PersonalId, customer.Email, customer.Telephone,
            customer.IsActive, customer.CreatedAt, customer.CreatedUserId, customer.LastUpdatedUserId);
}
