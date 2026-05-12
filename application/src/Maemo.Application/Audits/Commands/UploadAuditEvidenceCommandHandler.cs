using Maemo.Application.Common;
using Maemo.Domain.Audits;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Maemo.Application.Audits.Commands;

public class UploadAuditEvidenceCommandHandler : IRequestHandler<UploadAuditEvidenceCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly IFileHashService _fileHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public UploadAuditEvidenceCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IFileStorageProvider fileStorageProvider,
        IFileHashService fileHashService,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _fileStorageProvider = fileStorageProvider;
        _fileHashService = fileHashService;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task<string> Handle(UploadAuditEvidenceCommand command, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Verify audit run exists and belongs to current tenant
        var auditRun = await _context.AuditRuns
            .Where(r => r.Id == command.AuditRunId && r.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (auditRun == null)
        {
            throw new KeyNotFoundException($"Audit run with Id {command.AuditRunId} not found for current tenant.");
        }

        if (auditRun.CompletedAt.HasValue)
        {
            throw new ConflictException("Cannot modify a completed audit run.");
        }

        // Verify audit question exists and belongs to the template
        var question = await _context.AuditQuestions
            .Where(q => q.Id == command.AuditQuestionId && q.AuditTemplateId == auditRun.AuditTemplateId)
            .FirstOrDefaultAsync(cancellationToken);

        if (question == null)
        {
            throw new KeyNotFoundException($"Audit question with Id {command.AuditQuestionId} not found for this audit template.");
        }

        // Compute file hash for integrity verification
        // Create a copy of the stream for hashing (since SaveAsync will consume the stream)
        string? fileHash = null;
        Stream hashStream = command.FileContent;
        if (!command.FileContent.CanSeek)
        {
            // If stream is not seekable, copy to MemoryStream
            var memoryStream = new MemoryStream();
            await command.FileContent.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            hashStream = memoryStream;
        }
        else
        {
            // Save current position
            var originalPosition = command.FileContent.Position;
            command.FileContent.Position = 0;
        }

        fileHash = await _fileHashService.ComputeSha256HashAsync(hashStream, cancellationToken);

        // Reset stream position for upload
        if (command.FileContent.CanSeek)
        {
            command.FileContent.Position = 0;
        }
        else if (hashStream is MemoryStream ms)
        {
            ms.Position = 0;
        }

        // Use file storage provider (automatically selects local or SharePoint based on deployment mode)
        var category = $"Audits/{auditRun.Id}/Evidence";
        var uploadStream = hashStream is MemoryStream ? hashStream : command.FileContent;
        var evidenceFileUrl = await _fileStorageProvider.SaveAsync(
            tenantId,
            uploadStream,
            command.FileName,
            category,
            cancellationToken);

        // Find or create audit answer
        var existingAnswer = await _context.AuditAnswers
            .FirstOrDefaultAsync(
                a => a.AuditRunId == command.AuditRunId && 
                     a.AuditQuestionId == command.AuditQuestionId &&
                     a.TenantId == tenantId,
                cancellationToken);

        if (existingAnswer != null)
        {
            // Update existing answer with evidence file URL and hash
            existingAnswer.EvidenceFileUrl = evidenceFileUrl;
            existingAnswer.EvidenceFileHash = fileHash;
            existingAnswer.ModifiedAt = _dateTimeProvider.UtcNow;
            existingAnswer.ModifiedBy = _currentUserService.UserId;
        }
        else
        {
            // Create new answer with evidence file URL and hash
            var answer = new AuditAnswer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AuditRunId = command.AuditRunId,
                AuditQuestionId = command.AuditQuestionId,
                Score = 0, // Default score, can be updated later
                EvidenceFileUrl = evidenceFileUrl,
                EvidenceFileHash = fileHash,
                CreatedAt = _dateTimeProvider.UtcNow,
                CreatedBy = _currentUserService.UserId
            };

            _context.AuditAnswers.Add(answer);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "AuditRun.EvidenceUploaded",
            "AuditRun",
            command.AuditRunId.ToString(),
            new { AuditQuestionId = command.AuditQuestionId, FileName = command.FileName, EvidenceFileUrl = evidenceFileUrl },
            cancellationToken);

        return evidenceFileUrl;
    }
}

