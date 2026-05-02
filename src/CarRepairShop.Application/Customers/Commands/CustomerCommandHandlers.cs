using CarRepairShop.Application.Common.Exceptions;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Interfaces.Repositories;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;

namespace CarRepairShop.Application.Customers.Commands;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _customerRepository = customerRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var normalizedPersonalId = new string(request.PersonalId.Where(char.IsDigit).ToArray());

        if (await _customerRepository.ExistsByDocumentAsync(normalizedPersonalId, cancellationToken))
            throw new BusinessException($"A customer with personal ID '{request.PersonalId}' already exists.");

        var passwordHash = _passwordHasher.Hash("Mudar@123");
        var customer = new Customer(
            request.Name,
            request.PersonalId,
            request.Email,
            request.Telephone,
            passwordHash,
            _currentUserService.UserId);

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return CustomerMapper.MapToDto(customer);
    }
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        customer.Update(request.Name, request.Email, request.Telephone, _currentUserService.UserId);
        _customerRepository.Update(customer);
        await _unitOfWork.CommitAsync(cancellationToken);

        return CustomerMapper.MapToDto(customer);
    }
}

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, Unit>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        if (await _customerRepository.HasServiceOrdersAsync(request.Id, cancellationToken))
            throw new BusinessException("This customer cannot be deleted because they are linked to one or more service orders.");

        if (await _customerRepository.HasVehiclesAsync(request.Id, cancellationToken))
            throw new BusinessException("This customer cannot be deleted because they still have one or more vehicles linked.");

        _customerRepository.Delete(customer);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Unit.Value;
    }
}

file static class CustomerMapper
{
    internal static CustomerDto MapToDto(Customer customer) =>
        new(customer.Id, customer.Name, customer.PersonalId, customer.Email, customer.Telephone,
            customer.IsActive, customer.CreatedAt, customer.CreatedUserId, customer.LastUpdatedUserId);
}
