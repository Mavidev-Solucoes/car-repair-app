using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.Customers.Commands;

public record CreateCustomerCommand(string Name, string Email, string Phone, string Document) : IRequest<CustomerDto>;

public record UpdateCustomerCommand(Guid Id, string Name, string Email, string Phone) : IRequest<CustomerDto>;

public record DeleteCustomerCommand(Guid Id) : IRequest<Unit>;
