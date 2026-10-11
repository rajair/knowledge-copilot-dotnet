namespace KnowledgeCopilot.Contracts.Widgets;

/// <summary>A table comparing items across the same columns.</summary>
public sealed record ComparisonTableWidget : Widget
{
    /// <summary>Table title.</summary>
    public required string Title { get; init; }

    /// <summary>Column headers (excluding the row label column).</summary>
    public required IReadOnlyList<string> Columns { get; init; }

    /// <summary>Rows; each has one cell per column.</summary>
    public required IReadOnlyList<ComparisonRow> Rows { get; init; }

    /// <summary>Retrieved chunks the cells come from.</summary>
    public required IReadOnlyList<string> SourceChunkIds { get; init; }
}

/// <summary>One labelled row of a comparison table.</summary>
public sealed record ComparisonRow
{
    /// <summary>Row label.</summary>
    public required string Label { get; init; }

    /// <summary>Cell texts, in column order.</summary>
    public required IReadOnlyList<string> Cells { get; init; }
}
