using CarRepairShop.Application.DTOs;
using MediatR;

namespace CarRepairShop.Application.Auth.Commands;

public record LoginCommand(string Cpf) : IRequest<LoginResponseDto>;
