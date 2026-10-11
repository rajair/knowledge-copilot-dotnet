namespace KnowledgeCopilot.Contracts.Widgets;

/// <summary>
/// A chart of financial values. Values are <c>decimal</c> in C# and plain JSON numbers on the wire;
/// JavaScript and Python read them as binary floats, so use them for display only and keep any money
/// arithmetic on the server.
/// </summary>
public sealed record FinancialChartWidget : Widget
{
    /// <summary>Chart title.</summary>
    public required string Title { get; init; }

    /// <summary>Unit of every value, for example <c>USD millions</c>.</summary>
    public required string Unit { get; init; }

    /// <summary>One or more series sharing the same periods.</summary>
    public required IReadOnlyList<ChartSeries> Series { get; init; }

    /// <summary>Retrieved chunks the values come from.</summary>
    public required IReadOnlyList<string> SourceChunkIds { get; init; }
}

/// <summary>A named series of points.</summary>
public sealed record ChartSeries
{
    /// <summary>Series label, for example <c>Revenue</c>.</summary>
    public required string Label { get; init; }

    /// <summary>Points in period order.</summary>
    public required IReadOnlyList<ChartPoint> Points { get; init; }
}

/// <summary>One value for one period.</summary>
public sealed record ChartPoint
{
    /// <summary>Period label, for example <c>FY2024</c> or <c>2024-Q3</c>.</summary>
    public required string Period { get; init; }

    /// <summary>Value in the chart's unit.</summary>
    public required decimal Value { get; init; }
}
