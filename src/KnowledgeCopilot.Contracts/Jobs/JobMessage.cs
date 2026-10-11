namespace KnowledgeCopilot.Contracts.Jobs;

/// <summary>
/// Queue message from the api to the worker. Carries ids only, never content or ACLs: the worker
/// reads current state from the database, so a stale message cannot apply stale data.
/// </summary>
public sealed record JobMessage
{
    /// <summary>Ingestion job id (idempotency key).</summary>
    public required Guid JobId { get; init; }

    /// <summary>What to do.</summary>
    public required JobKind Kind { get; init; }

    /// <summary>Target document.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>Tenant that owns the document.</summary>
    public required string TenantId { get; init; }

    /// <summary>W3C <c>traceparent</c> of the request that created the job, if any.</summary>
    public string? TraceParent { get; init; }
}
