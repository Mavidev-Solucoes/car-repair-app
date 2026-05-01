using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.ServiceJobs.Commands;

public record UpdateServiceJobCommand(Guid Id, string Name, string Description, int UnitCost) : IRequest<ServiceJobDto>;

public record DeleteServiceJobCommand(Guid Id) : IRequest<Unit>;

public record AcknowledgeJobCommand(Guid Id) : IRequest<ServiceJobDto>;

public record StartJobProgressCommand(Guid Id) : IRequest<ServiceJobDto>;

public record CompleteJobCommand(Guid Id) : IRequest<ServiceJobDto>;
