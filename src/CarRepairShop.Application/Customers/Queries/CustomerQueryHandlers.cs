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

        return new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Phone, customer.Document, customer.CreatedAt);
    }
}

public class GetAllCustomersQueryHandler : IRequestHandler<GetAllCustomersQuery, IEnumerable<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;

    public GetAllCustomersQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<IEnumerable<CustomerDto>> Handle(GetAllCustomersQuery request, CancellationToken cancellationToken)
    {
        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        return customers.Select(c => new CustomerDto(c.Id, c.Name, c.Email, c.Phone, c.Document, c.CreatedAt));
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

        return new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Phone, customer.Document, customer.CreatedAt);
    }
}
