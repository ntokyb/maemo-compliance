using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.AccessRequests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.AccessRequests.Commands;

public class RejectAccessRequestCommandHandler : IRequestHandler<RejectAccessRequestCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public RejectAccessRequestCommandHandler(
        IApplicationDbContext context,
        IEmailSender emailSender,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _context = context;
        _emailSender = emailSender;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(RejectAccessRequestCommand request, CancellationToken cancellationToken)
    {
        var ar = await _context.AccessRequests
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Access request not found.");

        if (ar.Status != AccessRequestStatus.Pending)
        {
            throw new InvalidOperationException("Request is not pending.");
        }

        ar.Status = AccessRequestStatus.Rejected;
        ar.ReviewedAt = _clock.UtcNow;
        ar.ReviewedBy = _currentUser.UserEmail ?? _currentUser.UserId;
        ar.RejectionReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        var reasonLine = string.IsNullOrEmpty(ar.RejectionReason) ? "" : $"\nReason: {ar.RejectionReason}\n";
        var body =
            $"Hi {ar.ContactName},\n\n" +
            "Thank you for your interest in Maemo Compliance.\n" +
            "We are unable to approve your access request at this time." +
            reasonLine +
            "\nIf you believe this is an error, please reply to this thread or contact support.\n";

        await _emailSender.SendAsync(
            ar.ContactEmail,
            "Update on your Maemo Compliance request",
            body,
            cancellationToken);
    }
}
