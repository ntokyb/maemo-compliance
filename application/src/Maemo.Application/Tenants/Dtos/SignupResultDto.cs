namespace Maemo.Application.Tenants.Dtos;

public sealed class SignupResultDto
{
    public Guid TenantId { get; init; }
    public string Message { get; init; } = null!;
    public string NextStep { get; init; } = null!;
}
