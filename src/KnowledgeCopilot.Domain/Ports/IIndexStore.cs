using KnowledgeCopilot.Domain.Indexing;
using KnowledgeCopilot.Domain.Security;

namespace KnowledgeCopilot.Domain.Ports;

/// <summary>
/// The search index (D-04). Every search takes an <see cref="AclFilter"/> that the adapter pushes into
/// the engine query; no adapter may return a chunk the filter does not allow, and none may filter
/// after retrieval. Semantics are pinned by <c>IndexStoreContract</c>.
/// </summary>
public interface IIndexStore
{
    IndexCapabilities Capabilities { get; }

    /// <summary>Creates the index if it does not exist. Idempotent.</summary>
    Task EnsureIndexAsync(CancellationToken cancellationToken);

    /// <summary>Inserts or replaces chunks by <see cref="IndexedChunk.ChunkId"/> (payload and vectors).</summary>
    Task UpsertAsync(IReadOnlyList<IndexedChunk> chunks, CancellationToken cancellationToken);

    /// <summary>Removes every chunk of the document in that tenant only.</summary>
    Task DeleteByDocumentAsync(string tenantId, Guid documentId, CancellationToken cancellationToken);

    /// <summary>Removes the given chunks in that tenant only. Missing ids are ignored.</summary>
    Task DeleteChunksAsync(string tenantId, IReadOnlyList<string> chunkIds, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces <c>acl_principals</c> and <c>acl_version</c> on every chunk of the document without
    /// touching vectors or text (D-14).
    /// </summary>
    Task UpdateAclAsync(
        string tenantId,
        Guid documentId,
        IReadOnlyList<string> aclPrincipals,
        int aclVersion,
        CancellationToken cancellationToken);

    /// <summary>Top <paramref name="k"/> by vector similarity, score descending, ties by chunk id (ordinal).</summary>
    Task<IReadOnlyList<ScoredChunk>> SearchDenseAsync(
        ReadOnlyMemory<float> vector,
        AclFilter filter,
        int k,
        CancellationToken cancellationToken);

    /// <summary>Top <paramref name="k"/> by keyword relevance, score descending, ties by chunk id (ordinal).</summary>
    Task<IReadOnlyList<ScoredChunk>> SearchSparseAsync(
        string text,
        AclFilter filter,
        int k,
        CancellationToken cancellationToken);

    /// <summary>
    /// Engine-side fusion of dense and keyword search. Only when <see cref="IndexCapabilities.NativeHybrid"/>
    /// is true; otherwise throws <see cref="NotSupportedException"/>.
    /// </summary>
    Task<IReadOnlyList<ScoredChunk>> SearchHybridAsync(
        string text,
        ReadOnlyMemory<float> vector,
        AclFilter filter,
        int k,
        CancellationToken cancellationToken);
}
