using KnowledgeCopilot.Contracts.Serialization;

namespace KnowledgeCopilot.Contracts.Tests;

/// <summary>
/// Shared JSON examples in contracts/fixtures. File names are <c>&lt;RootType&gt;.&lt;case&gt;.json</c>;
/// the TS and Python test suites read the same files.
/// </summary>
internal static class Fixtures
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "fixtures");

    public static TheoryData<string> Valid => Names("valid");

    public static TheoryData<string> Invalid => Names("invalid");

    public static string Read(string kind, string name) => File.ReadAllText(Path.Combine(Root, kind, name));

    public static IEnumerable<string> Files(string kind) =>
        Directory.EnumerateFiles(Path.Combine(Root, kind), "*.json").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal);

    public static Type RootType(string fileName)
    {
        var typeName = fileName[..fileName.IndexOf('.', StringComparison.Ordinal)];
        return ContractsJsonContext.RootTypes.SingleOrDefault(t => t.Name == typeName)
            ?? throw new InvalidOperationException($"Fixture '{fileName}' does not name a root contract type.");
    }

    private static TheoryData<string> Names(string kind) => [.. Files(kind)];
}
