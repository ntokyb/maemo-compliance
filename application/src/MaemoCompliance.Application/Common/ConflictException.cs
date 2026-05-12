namespace MaemoCompliance.Application.Common;

/// <summary>
/// Thrown when an operation cannot complete due to a domain conflict (e.g. deleting a closed NCR).
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
