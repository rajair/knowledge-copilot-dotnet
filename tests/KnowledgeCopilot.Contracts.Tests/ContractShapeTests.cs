using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KnowledgeCopilot.Contracts.Search;
using KnowledgeCopilot.Contracts.Serialization;
using KnowledgeCopilot.Contracts.Widgets;

namespace KnowledgeCopilot.Contracts.Tests;

public sealed class ContractShapeTests
{
    [Fact]
    public void ActionType_EachValue_HasArgumentsSubtypeWithSameDiscriminator()
    {
        var discriminators = typeof(ActionArguments).GetCustomAttributes<JsonDerivedTypeAttribute>()
            .Select(a => (string)a.TypeDiscriminator!)
            .Order(StringComparer.Ordinal);
        var actionTypes = Enum.GetValues<ActionType>()
            .Select(v => JsonNamingPolicy.CamelCase.ConvertName(v.ToString()))
            .Order(StringComparer.Ordinal);

        Assert.Equal(actionTypes, discriminators);
    }

    [Fact]
    public void Widget_EachDerivedType_HasValidFixture()
    {
        var discriminators = typeof(Widget).GetCustomAttributes<JsonDerivedTypeAttribute>().Select(a => (string)a.TypeDiscriminator!);
        var fixtures = Fixtures.Files("valid").Where(f => f.StartsWith("Widget.", StringComparison.Ordinal)).ToList();

        Assert.All(discriminators, d => Assert.Contains(fixtures, f => f.StartsWith($"Widget.{d}", StringComparison.Ordinal)));
    }

    [Fact]
    public void SearchRequest_OptionalFieldsOmitted_UsesDefaults()
    {
        var request = JsonSerializer.Deserialize(Fixtures.Read("valid", "SearchRequest.defaults.json"), ContractsJsonContext.Default.SearchRequest)!;

        Assert.Equal(8, request.TopK);
        Assert.Equal(SearchMode.Hybrid, request.Mode);
    }

    [Fact]
    public void ContractsVersion_Value_IsSemanticVersion()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+$", ContractsVersion.Value);
    }
}
