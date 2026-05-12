namespace Maemo.Application.Common;

public record UserProfileDto(
    string DisplayName,
    string UserPrincipalName,
    string? JobTitle,
    string? Department
);

