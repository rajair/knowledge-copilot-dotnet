using KnowledgeCopilot.Contracts.Serialization;
using KnowledgeCopilot.SchemaExport;

// Writes contracts/schema/<Name>.schema.json for every root contract (D-1-06).
// Usage: dotnet run --project tools/KnowledgeCopilot.SchemaExport -- <output directory>
if (args.Length != 1)
{
    await Console.Error.WriteLineAsync("Usage: KnowledgeCopilot.SchemaExport <output directory>").ConfigureAwait(false);
    return 2;
}

var outputDirectory = Path.GetFullPath(args[0]);
var docs = XmlDocDescriptions.Load(typeof(ContractsJsonContext).Assembly);
var written = SchemaWriter.WriteAll(ContractsJsonContext.RootTypes, docs, outputDirectory);
Console.WriteLine($"Wrote {written} JSON Schema files to {outputDirectory}");
return 0;
