using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Interfaces.Services;
using MediatR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CarRepairShop.Application.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var token = await _jwtService.GenerateTokenAsync(request.Cpf, cancellationToken);
        var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var name = parsedToken.Claims.FirstOrDefault(c =>
            c.Type == JwtRegisteredClaimNames.Name || c.Type == "name")?.Value ?? string.Empty;
        var role = parsedToken.Claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Role || c.Type == "role")?.Value ?? "Customer";

        return new LoginResponseDto(token, request.Cpf, name, role);
    }
}
