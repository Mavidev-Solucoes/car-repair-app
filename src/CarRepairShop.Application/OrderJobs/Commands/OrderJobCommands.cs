using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.OrderJobs.Commands;

public record AcknowledgeOrderJobCommand(Guid Id) : IRequest<ServiceOrderJobDto>;

public record StartOrderJobProgressCommand(Guid Id) : IRequest<ServiceOrderJobDto>;

public record CompleteOrderJobCommand(Guid Id) : IRequest<ServiceOrderJobDto>;
