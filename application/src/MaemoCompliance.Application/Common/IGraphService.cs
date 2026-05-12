namespace MaemoCompliance.Application.Common;

public interface IGraphService
{
    Task UploadDocumentAsync(
        Guid tenantId,
        Stream content,
        string fileName,
        string folderPath,
        CancellationToken cancellationToken = default
    );

    Task SendTeamsMessageAsync(
        Guid tenantId,
        string teamId,
        string channelId,
        string message,
        CancellationToken cancellationToken = default
    );

    Task SendMailAsync(
        Guid tenantId,
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default
    );

    Task<UserProfileDto?> GetUserProfileAsync(
        Guid tenantId,
        string userPrincipalName,
        CancellationToken cancellationToken = default
    );
}

