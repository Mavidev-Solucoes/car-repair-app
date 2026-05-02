using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.ServiceJobs.Commands;

public record CreateServiceJobCommand(string Name, string Description, decimal Price) : IRequest<ServiceJobDto>;

public record UpdateServiceJobCommand(Guid Id, string Name, string Description, decimal Price) : IRequest<ServiceJobDto>;

public record DeleteServiceJobCommand(Guid Id) : IRequest<Unit>;
