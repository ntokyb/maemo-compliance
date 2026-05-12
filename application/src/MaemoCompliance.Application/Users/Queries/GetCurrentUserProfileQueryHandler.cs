using System.Text.Json;
using MaemoCompliance.Application.Common;
using MediatR;using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Users.Queries;

public class GetCurrentUserProfileQueryHandler : IRequestHandler<GetCurrentUserProfileQuery, UserProfileDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserProfileQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUser)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _currentUser = currentUser;
    }

    public async Task<UserProfileDto?> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.HasTenant())
        {
            return null;
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var email = _currentUser.UserEmail;
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var emailNorm = email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId && u.Email.ToLower() == emailNorm,
                cancellationToken);

        if (user == null)
        {
            return null;
        }

        return new UserProfileDto(
            user.Email,
            user.FullName,
            user.OnboardingComplete,
            user.JobTitle,
            user.Phone,
            user.AddressLine,
            DeserializeStandards(user.ComplianceStandardsJson));
    }

    private static IReadOnlyList<string> DeserializeStandards(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
