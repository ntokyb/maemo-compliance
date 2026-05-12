namespace MaemoCompliance.Domain.Documents;

/// <summary>
/// Represents the workflow state of a document in the approval process.
/// State transitions: Draft → PendingApproval → Approved → Active → Archived
/// </summary>
public enum DocumentWorkflowState
{
    /// <summary>
    /// Document is in draft state and can be edited.
    /// Can transition to: PendingApproval
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Document has been submitted for approval and is awaiting review.
    /// Can transition to: Approved, Draft (if rejected)
    /// </summary>
    PendingApproval = 1,

    /// <summary>
    /// Document has been approved but not yet activated.
    /// Can transition to: Active
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Document is active and in use.
    /// Can transition to: Archived
    /// </summary>
    Active = 3,

    /// <summary>
    /// Document has been archived and is no longer active.
    /// Terminal state - cannot transition from this state.
    /// </summary>
    Archived = 4,

    /// <summary>
    /// A newer approved version exists; this record is retained for history only.
    /// </summary>
    Obsolete = 5,
}

