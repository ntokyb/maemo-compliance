using Maemo.Application.Tenants.Dtos;
using MediatR;

namespace Maemo.Application.Tenants.Commands;

public sealed record SignupCommand(
    string CompanyName,
    string AdminEmail,
    string AdminFirstName,
    string AdminLastName,
    string Industry,
    string Plan,
    IReadOnlyList<string> IsoFrameworks,
    string? ClientIp) : IRequest<SignupResultDto>;
