using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Xml.Linq;

namespace KnowledgeCopilot.SchemaExport;

/// <summary>
/// Reads the Contracts XML documentation file and turns <c>summary</c> (or the record's <c>param</c>)
/// into schema <c>description</c>s, rendered the same way as in the OpenAPI document.
/// </summary>
internal sealed class XmlDocDescriptions
{
    private readonly Dictionary<string, XElement> _members;

    private XmlDocDescriptions(Dictionary<string, XElement> members) => _members = members;

    public static XmlDocDescriptions Load(Assembly assembly)
    {
        var path = Path.ChangeExtension(assembly.Location, ".xml");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The Contracts XML documentation file is missing; it must be copied next to the tool.", path);
        }

        var members = XDocument.Load(path).Descendants("member")
            .Where(member => member.Attribute("name") is not null)
            .ToDictionary(member => member.Attribute("name")!.Value, StringComparer.Ordinal);
        return new XmlDocDescriptions(members);
    }

    public JsonNode AddDescription(JsonSchemaExporterContext context, JsonNode schema)
    {
        if (schema is not JsonObject node)
        {
            return schema;
        }

        var description = context.PropertyInfo is { } property
            ? DescribeProperty(property.DeclaringType, property.AttributeProvider as MemberInfo)
            : DescribeType(context.TypeInfo.Type);
        if (description is not null && !node.ContainsKey("$ref"))
        {
            node["description"] = description;
        }

        return node;
    }

    private string? DescribeType(Type type) =>
        _members.TryGetValue("T:" + DocId(type), out var member) ? Render(member.Element("summary")) : null;

    private string? DescribeProperty(Type declaringType, MemberInfo? member)
    {
        if (member is null)
        {
            return null;
        }

        var owner = member.DeclaringType ?? declaringType;
        if (_members.TryGetValue("P:" + DocId(owner) + "." + member.Name, out var property))
        {
            return Render(property.Element("summary"));
        }

        // Positional records document their properties as <param> on the type.
        return _members.TryGetValue("T:" + DocId(owner), out var type)
            ? Render(type.Elements("param").FirstOrDefault(param => (string?)param.Attribute("name") == member.Name))
            : null;
    }

    private static string DocId(Type type) => type.FullName!.Replace('+', '.');

    private static string? Render(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        var text = new StringBuilder();
        foreach (var node in element.Nodes())
        {
            text.Append(node switch
            {
                XText value => value.Value,
                XElement { Name.LocalName: "c" } code => $"`{code.Value}`",
                XElement { Name.LocalName: "see" } see => ShortName((string?)see.Attribute("cref") ?? see.Value),
                XElement other => other.Value,
                _ => string.Empty,
            });
        }

        var lines = text.ToString().ReplaceLineEndings("\n").Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);
        var rendered = string.Join("\n", lines);
        return rendered.Length > 0 ? rendered : null;
    }

    // "T:Ns.Type" -> "Type"; members ("F:Ns.Type.Member", "P:...") -> "Type.Member".
    private static string ShortName(string cref)
    {
        var parts = cref[(cref.IndexOf(':', StringComparison.Ordinal) + 1)..].Split('.');
        return cref.StartsWith("T:", StringComparison.Ordinal) || parts.Length < 2 ? parts[^1] : $"{parts[^2]}.{parts[^1]}";
    }
}
