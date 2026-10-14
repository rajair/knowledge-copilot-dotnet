namespace KnowledgeCopilot.Domain.Indexing;

/// <summary>What an index adapter can do natively; the retriever chooses its strategy from this (architecture §6).</summary>
/// <param name="NativeHybrid">
/// <see cref="Ports.IIndexStore.SearchHybridAsync"/> is supported. When false it throws
/// <see cref="NotSupportedException"/> and the retriever fuses dense and sparse results itself.
/// </param>
/// <param name="SparseVectors">Keyword search uses stored sparse vectors (Qdrant) rather than a built-in BM25 index.</param>
public sealed record IndexCapabilities(bool NativeHybrid, bool SparseVectors);
