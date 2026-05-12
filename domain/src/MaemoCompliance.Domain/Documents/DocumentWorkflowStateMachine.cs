namespace MaemoCompliance.Domain.Documents;

/// <summary>
/// State machine helper for document workflow transitions.
/// Enforces the workflow: Draft → PendingApproval → Approved → Active → Archived
/// </summary>
public static class DocumentWorkflowStateMachine
{
    /// <summary>
    /// Checks if a transition from one workflow state to another is allowed.
    /// </summary>
    /// <param name="from">Current workflow state</param>
    /// <param name="to">Target workflow state</param>
    /// <returns>True if transition is allowed, false otherwise</returns>
    public static bool CanTransition(DocumentWorkflowState from, DocumentWorkflowState to)
    {
        // Same state is always allowed (no-op)
        if (from == to)
        {
            return true;
        }

        return from switch
        {
            // Draft can transition to: PendingApproval
            DocumentWorkflowState.Draft => to == DocumentWorkflowState.PendingApproval,

            // PendingApproval can transition to: Approved (approved), Draft (rejected)
            DocumentWorkflowState.PendingApproval => to == DocumentWorkflowState.Approved || 
                                                      to == DocumentWorkflowState.Draft,

            // Approved can transition to: Active
            DocumentWorkflowState.Approved => to == DocumentWorkflowState.Active,

            // Active can transition to: Archived
            DocumentWorkflowState.Active => to == DocumentWorkflowState.Archived,

            // Archived is terminal - no transitions allowed
            DocumentWorkflowState.Archived => false,

            _ => false
        };
    }

    /// <summary>
    /// Gets the allowed target states from the current state.
    /// </summary>
    /// <param name="currentState">Current workflow state</param>
    /// <returns>Array of allowed target states</returns>
    public static DocumentWorkflowState[] GetAllowedTransitions(DocumentWorkflowState currentState)
    {
        return currentState switch
        {
            DocumentWorkflowState.Draft => new[] { DocumentWorkflowState.PendingApproval },
            DocumentWorkflowState.PendingApproval => new[] { DocumentWorkflowState.Approved, DocumentWorkflowState.Draft },
            DocumentWorkflowState.Approved => new[] { DocumentWorkflowState.Active },
            DocumentWorkflowState.Active => new[] { DocumentWorkflowState.Archived },
            DocumentWorkflowState.Archived => Array.Empty<DocumentWorkflowState>(),
            _ => Array.Empty<DocumentWorkflowState>()
        };
    }

    /// <summary>
    /// Checks if a document can be edited in the current workflow state.
    /// </summary>
    /// <param name="state">Current workflow state</param>
    /// <returns>True if document can be edited, false otherwise</returns>
    public static bool CanEdit(DocumentWorkflowState state)
    {
        return state == DocumentWorkflowState.Draft;
    }

    /// <summary>
    /// Maps DocumentWorkflowState to DocumentStatus for backward compatibility.
    /// </summary>
    /// <param name="workflowState">Workflow state</param>
    /// <returns>Corresponding DocumentStatus</returns>
    public static DocumentStatus MapToDocumentStatus(DocumentWorkflowState workflowState)
    {
        return workflowState switch
        {
            DocumentWorkflowState.Draft => DocumentStatus.Draft,
            DocumentWorkflowState.PendingApproval => DocumentStatus.UnderReview,
            DocumentWorkflowState.Approved => DocumentStatus.Active, // Approved is treated as Active
            DocumentWorkflowState.Active => DocumentStatus.Active,
            DocumentWorkflowState.Archived => DocumentStatus.Archived,
            _ => DocumentStatus.Draft
        };
    }
}

