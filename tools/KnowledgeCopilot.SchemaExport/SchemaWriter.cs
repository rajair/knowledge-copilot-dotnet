using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using KnowledgeCopilot.Contracts.Serialization;

namespace KnowledgeCopilot.SchemaExport;

/// <summary>Exports one JSON Schema per root contract with the same options the services serialize with.</summary>
internal static class SchemaWriter
{
    private const string FileSuffix = ".schema.json";
    private const string SchemaDialect = "https://json-schema.org/draft/2020-12/schema";

    private static readonly JsonSerializerOptions OutputOptions = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        // Files, not HTML: keep backticks, quotes and '+' readable.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static int WriteAll(IReadOnlyList<Type> rootTypes, XmlDocDescriptions docs, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        // Remove stale files so a deleted contract cannot linger and pass the drift check.
        foreach (var stale in Directory.EnumerateFiles(outputDirectory, "*" + FileSuffix))
        {
            File.Delete(stale);
        }

        var exporterOptions = new JsonSchemaExporterOptions
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = docs.AddDescription,
        };

        foreach (var type in rootTypes)
        {
            var typeInfo = ContractsJsonContext.Default.GetTypeInfo(type)
                ?? throw new InvalidOperationException($"{type.Name} is not registered in {nameof(ContractsJsonContext)}.");
            var schema = typeInfo.GetJsonSchemaAsNode(exporterOptions);
            var document = new JsonObject
            {
                ["$schema"] = SchemaDialect,
                ["title"] = type.Name,
            };
            foreach (var (key, value) in schema.AsObject().ToList())
            {
                schema.AsObject().Remove(key);
                document[key] = value;
            }

            var path = Path.Combine(outputDirectory, type.Name + FileSuffix);
            File.WriteAllText(path, document.ToJsonString(OutputOptions) + "\n");
        }

        return rootTypes.Count;
    }
}
