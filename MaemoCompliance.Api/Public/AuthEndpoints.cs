using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;
using MaemoCompliance.Api.Authentication;
using MaemoCompliance.Api.Common;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.AccessRequests;
using MaemoCompliance.Domain.Tenants;
using MaemoCompliance.Domain.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Api.Public;

/// <summary>Email/password authentication alongside Azure AD. All routes are anonymous except /me.</summary>
public static class AuthEndpoints
{
    public const string AuthTag = "Auth (local)";

    public sealed record RegisterBody(
        string CompanyName,
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string Plan,
        string? Industry,
        string? CompanySize,
        string[]? TargetStandards);

    public sealed record VerifyEmailBody(string Token);

    public sealed record LoginBody(string Email, string Password);

    public sealed record ResendVerificationBody(string Email);

    public sealed record ForgotPasswordBody(string Email);

    public sealed record ResetPasswordBody(string Token, string NewPassword);

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags(AuthTag);

        group.MapPost("/register", Register)
            .AllowAnonymous();

        group.MapPost("/verify-email", VerifyEmail)
            .AllowAnonymous();

        group.MapPost("/login", Login)
            .AllowAnonymous();

        group.MapPost("/resend-verification", ResendVerification)
            .AllowAnonymous();

        group.MapPost("/forgot-password", ForgotPassword)
            .AllowAnonymous();

        group.MapPost("/reset-password", ResetPassword)
            .AllowAnonymous();

        group.MapGet("/me", Me)
            .RequireAuthorization();
    }

    private static bool IsStrongPassword(string password)
    {
        if (password.Length < 8)
        {
            return false;
        }

        if (!password.Any(char.IsUpper) || !password.Any(char.IsDigit))
        {
            return false;
        }

        return true;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(email.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<IResult> Register(
        HttpContext httpContext,
        [FromBody] RegisterBody body,
        IApplicationDbContext db,
        IEmailSender emailSender,
        IConfiguration config,
        IPublicSignupRateLimiter rateLimiter,
        IDateTimeProvider clock,
        CancellationToken ct)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!rateLimiter.TryAllow(ip))
        {
            return Results.Json(
                new MaemoCompliance.Shared.Contracts.Common.ErrorResponse(
                    "TooManyRequests",
                    "Too many signup attempts from this network. Try again in an hour.",
                    null,
                    Guid.NewGuid().ToString()),
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        if (string.IsNullOrWhiteSpace(body.CompanyName))
        {
            return ErrorResults.BadRequest("InvalidSignup", "Company name is required.");
        }

        if (!IsValidEmail(body.Email))
        {
            return ErrorResults.BadRequest("InvalidSignup", "A valid email is required.");
        }

        if (!IsStrongPassword(body.Password))
        {
            return ErrorResults.BadRequest(
                "WeakPassword",
                "Password must be at least 8 characters and include one uppercase letter and one number.");
        }

        var planNorm = body.Plan.Trim();
        var isStarter = string.Equals(planNorm, "Starter", StringComparison.OrdinalIgnoreCase);
        var isGrowth = string.Equals(planNorm, "Growth", StringComparison.OrdinalIgnoreCase);
        if (!isStarter && !isGrowth)
        {
            return ErrorResults.BadRequest("InvalidPlan", "Plan must be Starter or Growth.");
        }

        var email = body.Email.Trim();
        var emailNorm = email.ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email.ToLower() == emailNorm, ct)
            || await db.Tenants.AnyAsync(t => t.AdminEmail.ToLower() == emailNorm, ct))
        {
            return ErrorResults.Conflict("DuplicateSignup", "An account already exists for this email.");
        }

        var fullName = $"{body.FirstName.Trim()} {body.LastName.Trim()}".Trim();
        if (string.IsNullOrEmpty(fullName))
        {
            fullName = email;
        }

        var now = clock.UtcNow;
        var verifyToken = Guid.NewGuid().ToString("N");
        var standards = (body.TargetStandards ?? Array.Empty<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        var standardsJson = JsonSerializer.Serialize(standards);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = body.CompanyName.Trim(),
            AdminEmail = email,
            Plan = isStarter ? "Starter" : "Professional",
            Edition = "Standard",
            IsActive = isStarter,
            CreatedAt = now,
            CreatedBy = "AuthRegister",
            MaxUsers = isStarter ? 5 : 500,
            MaxStorageBytes = isStarter ? 1_073_741_824L : 107_374_182_400L,
            ModulesEnabledJson = JsonSerializer.Serialize(new[] { "Documents", "NCR", "Risks", "Audits" }),
            TrialEndsAt = isStarter ? now.AddDays(14) : null,
            TargetStandardsJson = standardsJson,
            Industry = string.IsNullOrWhiteSpace(body.Industry) ? null : body.Industry.Trim(),
            CompanySize = string.IsNullOrWhiteSpace(body.CompanySize) ? null : body.CompanySize.Trim()
        };

        db.Tenants.Add(tenant);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = email,
            FullName = fullName,
            Role = UserRole.TenantAdmin,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = "AuthRegister",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password),
            EmailVerificationToken = verifyToken,
            EmailVerified = false,
            AuthProvider = "Local",
            ComplianceStandardsJson = standardsJson
        };
        db.Users.Add(user);

        if (isGrowth)
        {
            if (await db.AccessRequests.AnyAsync(
                    a => a.ContactEmail.ToLower() == emailNorm && a.Status == AccessRequestStatus.Pending,
                    ct))
            {
                return ErrorResults.Conflict("DuplicatePending", "A pending request already exists for this email.");
            }

            var ar = new AccessRequest
            {
                Id = Guid.NewGuid(),
                CompanyName = body.CompanyName.Trim(),
                Industry = string.IsNullOrWhiteSpace(body.Industry) ? "Other" : body.Industry.Trim(),
                CompanySize = string.IsNullOrWhiteSpace(body.CompanySize) ? "Unknown" : body.CompanySize.Trim(),
                ContactName = fullName,
                ContactEmail = email,
                ContactRole = "Administrator",
                TargetStandardsJson = standardsJson,
                ReferralSource = "SelfServiceGrowthSignup",
                Status = AccessRequestStatus.Pending,
                CreatedTenantId = tenant.Id,
                CreatedAt = now,
                CreatedBy = "AuthRegister"
            };
            db.AccessRequests.Add(ar);

            var adminTo = config["App:AdminNotificationEmail"] ?? "admin@maemo-compliance.co.za";
            await emailSender.SendAsync(
                adminTo,
                $"Growth signup pending — {tenant.Name}",
                $"Company: {tenant.Name}\nEmail: {email}\nAccess request id: {ar.Id}\nTenant id: {tenant.Id}",
                ct);
        }

        await db.SaveChangesAsync(ct);

        var baseUrl = (config["App:PublicPortalUrl"] ?? "").TrimEnd('/');
        var verifyPath =
            string.IsNullOrEmpty(baseUrl)
                ? $"/verify?token={verifyToken}&email={Uri.EscapeDataString(email)}"
                : $"{baseUrl}/verify?token={verifyToken}&email={Uri.EscapeDataString(email)}";

        await emailSender.SendAsync(
            email,
            "Verify your email — Maemo Compliance",
            $"Hi {body.FirstName.Trim()},\n\nVerify your email to continue: {verifyPath}\n\nLink expires in 48 hours.\n",
            ct);

        return Results.Ok(new
        {
            tenantId = tenant.Id,
            userId = user.Id,
            message = isGrowth
                ? "Your application was received. Check your email to verify your address. We will review your workspace within one business day."
                : "Check your email to verify your account.",
            plan = body.Plan,
            requiresReview = isGrowth
        });
    }

    private static async Task<IResult> VerifyEmail(
        [FromBody] VerifyEmailBody body,
        IApplicationDbContext db,
        LocalJwtTokenFactory jwtFactory,
        IDateTimeProvider clock,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Token))
        {
            return ErrorResults.BadRequest("InvalidToken", "Verification token is required.");
        }

        var token = body.Token.Trim();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, ct);

        if (user == null || user.TenantId is not Guid tenantId)
        {
            return ErrorResults.BadRequest("InvalidToken", "This verification link has expired or has already been used.");
        }

        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant == null)
        {
            return ErrorResults.BadRequest("InvalidToken", "Workspace not found.");
        }

        user.EmailVerified = true;
        user.EmailVerifiedAt = clock.UtcNow;
        user.EmailVerificationToken = null;
        user.ModifiedAt = clock.UtcNow;
        user.ModifiedBy = "VerifyEmail";

        await db.SaveChangesAsync(ct);

        var (jwt, exp) = jwtFactory.CreateToken(user, tenantId);
        return Results.Ok(new
        {
            token = jwt,
            expiresAt = exp,
            user = UserDto(user, tenant),
            tenant = TenantDto(tenant)
        });
    }

    private static async Task<IResult> Login(
        [FromBody] LoginBody body,
        IApplicationDbContext db,
        LocalJwtTokenFactory jwtFactory,
        IDateTimeProvider clock,
        CancellationToken ct)
    {
        var email = body.Email?.Trim() ?? "";
        var emailNorm = email.ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm, ct);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return ErrorResults.Unauthorized("InvalidCredentials", "Email or password incorrect.");
        }

        if (string.Equals(user.AuthProvider, "AzureAD", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorResults.BadRequest(
                "UseMicrosoft",
                "This account uses Microsoft login. Use “Sign in with Microsoft” instead.");
        }

        if (!user.EmailVerified)
        {
            return Results.Json(
                new
                {
                    code = "EmailNotVerified",
                    message = "Please verify your email first.",
                    resendAvailable = true
                },
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (!BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash))
        {
            return ErrorResults.Unauthorized("InvalidCredentials", "Email or password incorrect.");
        }

        if (user.TenantId is not Guid tenantId)
        {
            return ErrorResults.BadRequest("NoWorkspace", "Your account is not linked to a workspace.");
        }

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant == null)
        {
            return ErrorResults.BadRequest("NoWorkspace", "Your workspace could not be found.");
        }

        if (!tenant.IsActive)
        {
            return Results.Json(
                new
                {
                    code = "TenantPendingApproval",
                    message =
                        "Your workspace is still under review. We will email you when it is activated."
                },
                statusCode: StatusCodes.Status403Forbidden);
        }

        user.LastLoginAt = clock.UtcNow;
        user.ModifiedAt = clock.UtcNow;
        user.ModifiedBy = "Login";
        await db.SaveChangesAsync(ct);

        var (jwt, exp) = jwtFactory.CreateToken(user, tenantId);
        return Results.Ok(new
        {
            token = jwt,
            expiresAt = exp,
            user = UserDto(user, tenant),
            tenant = TenantDto(tenant)
        });
    }

    private static async Task<IResult> ResendVerification(
        [FromBody] ResendVerificationBody body,
        IApplicationDbContext db,
        IEmailSender emailSender,
        IConfiguration config,
        IDateTimeProvider clock,
        CancellationToken ct)
    {
        var email = body.Email?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(email))
        {
            return Results.Ok(new { ok = true });
        }

        var emailNorm = email.ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm, ct);
        if (user != null && !user.EmailVerified && user.AuthProvider == "Local")
        {
            user.EmailVerificationToken = Guid.NewGuid().ToString("N");
            user.ModifiedAt = clock.UtcNow;
            user.ModifiedBy = "ResendVerification";
            await db.SaveChangesAsync(ct);

            var baseUrl = (config["App:PublicPortalUrl"] ?? "").TrimEnd('/');
            var verifyPath = string.IsNullOrEmpty(baseUrl)
                ? $"/verify?token={user.EmailVerificationToken}&email={Uri.EscapeDataString(user.Email)}"
                : $"{baseUrl}/verify?token={user.EmailVerificationToken}&email={Uri.EscapeDataString(user.Email)}";

            var first = user.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "there";
            await emailSender.SendAsync(
                user.Email,
                "Verify your email — Maemo Compliance",
                $"Hi {first},\n\nVerify your email: {verifyPath}\n",
                ct);
        }

        return Results.Ok(new { ok = true });
    }

    private static async Task<IResult> ForgotPassword(
        [FromBody] ForgotPasswordBody body,
        IApplicationDbContext db,
        IEmailSender emailSender,
        IConfiguration config,
        IDateTimeProvider clock,
        CancellationToken ct)
    {
        var email = body.Email?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailNorm = email.ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm, ct);
            if (user != null
                && !string.IsNullOrEmpty(user.PasswordHash)
                && string.Equals(user.AuthProvider, "Local", StringComparison.OrdinalIgnoreCase))
            {
                var token = Guid.NewGuid().ToString("N");
                user.PasswordResetToken = token;
                user.PasswordResetExpiresAt = clock.UtcNow.AddHours(1);
                user.ModifiedAt = clock.UtcNow;
                user.ModifiedBy = "ForgotPassword";
                await db.SaveChangesAsync(ct);

                var baseUrl = (config["App:PublicPortalUrl"] ?? "").TrimEnd('/');
                var link = string.IsNullOrEmpty(baseUrl)
                    ? $"/reset-password?token={token}"
                    : $"{baseUrl}/reset-password?token={token}";

                await emailSender.SendAsync(
                    user.Email,
                    "Reset your Maemo Compliance password",
                    $"Reset your password (valid 1 hour): {link}\n",
                    ct);
            }
        }

        return Results.Ok(new
        {
            message = "If that email is registered, you will receive a link shortly."
        });
    }

    private static async Task<IResult> ResetPassword(
        [FromBody] ResetPasswordBody body,
        IApplicationDbContext db,
        IDateTimeProvider clock,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Token))
        {
            return ErrorResults.BadRequest("InvalidToken", "Reset token is required.");
        }

        if (!IsStrongPassword(body.NewPassword))
        {
            return ErrorResults.BadRequest(
                "WeakPassword",
                "Password must be at least 8 characters and include one uppercase letter and one number.");
        }

        var token = body.Token.Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token, ct);
        if (user == null || user.PasswordResetExpiresAt == null || user.PasswordResetExpiresAt < clock.UtcNow)
        {
            return ErrorResults.BadRequest("InvalidToken", "This reset link has expired or has already been used.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpiresAt = null;
        user.ModifiedAt = clock.UtcNow;
        user.ModifiedBy = "ResetPassword";
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { ok = true });
    }

    private static async Task<IResult> Me(
        HttpContext httpContext,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var principal = httpContext.User;
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            return Results.NotFound();
        }

        if (user.TenantId is not Guid tid)
        {
            return Results.Ok(new { user = UserDto(user, null), tenant = (object?)null });
        }

        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tid, ct);
        return Results.Ok(new { user = UserDto(user, tenant), tenant = tenant == null ? null : TenantDto(tenant) });
    }

    private static object UserDto(User user, Tenant? tenant) =>
        new
        {
            userId = user.Id,
            tenantId = user.TenantId,
            email = user.Email,
            firstName = user.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? user.FullName,
            lastName = user.FullName.Contains(' ', StringComparison.Ordinal)
                ? user.FullName[(user.FullName.IndexOf(' ', StringComparison.Ordinal) + 1)..].Trim()
                : "",
            role = (int)user.Role,
            authProvider = user.AuthProvider,
            fullName = user.FullName,
            tenant = tenant == null
                ? null
                : new { name = tenant.Name, plan = tenant.Plan, setupComplete = tenant.SetupComplete, setupStep = tenant.SetupStep }
        };

    private static object TenantDto(Tenant t) =>
        new
        {
            id = t.Id,
            name = t.Name,
            plan = t.Plan,
            setupComplete = t.SetupComplete,
            setupStep = t.SetupStep,
            logoUrl = t.LogoUrl,
            isActive = t.IsActive
        };
}
