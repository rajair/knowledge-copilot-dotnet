using System.Text.Json;
using System.Text.Json.Nodes;
using KnowledgeCopilot.Contracts.Serialization;

namespace KnowledgeCopilot.Contracts.Tests;

public sealed class RoundTripTests
{
    private static readonly ContractsJsonContext Context = ContractsJsonContext.Default;

    [Theory]
    [MemberData(nameof(Fixtures.Valid), MemberType = typeof(Fixtures))]
    public void Deserialize_ValidFixture_ReserializesToEquivalentJson(string fixture)
    {
        var type = Fixtures.RootType(fixture);
        var original = JsonNode.Parse(Fixtures.Read("valid", fixture))!;

        var value = JsonSerializer.Deserialize(original.ToJsonString(), type, Context);
        var written = JsonNode.Parse(JsonSerializer.Serialize(value, type, Context))!;
        var rewritten = JsonNode.Parse(JsonSerializer.Serialize(JsonSerializer.Deserialize(written.ToJsonString(), type, Context), type, Context))!;

        // Every property in the fixture survives unchanged; defaults and nulls may be added on write.
        AssertContains(original, written, "$");
        Assert.True(JsonNode.DeepEquals(written, rewritten), "Second round trip changed the JSON.");
    }

    [Theory]
    [MemberData(nameof(Fixtures.Invalid), MemberType = typeof(Fixtures))]
    public void Deserialize_InvalidFixture_Throws(string fixture)
    {
        var type = Fixtures.RootType(fixture);
        var json = Fixtures.Read("invalid", fixture);

        var exception = Record.Exception(() => JsonSerializer.Deserialize(json, type, Context));

        // Unknown or missing discriminators surface as NotSupportedException (the base type is abstract).
        Assert.True(exception is JsonException or NotSupportedException, $"Expected a JSON error, got {exception?.GetType().Name ?? "none"}.");
    }

    [Fact]
    public void RootTypes_EachType_HasValidAndInvalidFixtures()
    {
        var valid = Fixtures.Files("valid").Select(Fixtures.RootType).ToHashSet();
        var invalid = Fixtures.Files("invalid").Select(Fixtures.RootType).ToHashSet();

        Assert.All(ContractsJsonContext.RootTypes, t =>
        {
            Assert.Contains(t, valid);
            Assert.Contains(t, invalid);
        });
    }

    private static void AssertContains(JsonNode? expected, JsonNode? actual, string path)
    {
        switch (expected)
        {
            case JsonObject expectedObject:
                var actualObject = Assert.IsType<JsonObject>(actual);
                foreach (var (key, value) in expectedObject)
                {
                    Assert.True(actualObject.ContainsKey(key), $"{path}.{key} is missing after round trip.");
                    AssertContains(value, actualObject[key], $"{path}.{key}");
                }

                break;
            case JsonArray expectedArray:
                var actualArray = Assert.IsType<JsonArray>(actual);
                Assert.Equal(expectedArray.Count, actualArray.Count);
                for (var i = 0; i < expectedArray.Count; i++)
                {
                    AssertContains(expectedArray[i], actualArray[i], $"{path}[{i}]");
                }

                break;
            case JsonValue expectedValue when expectedValue.GetValueKind() == JsonValueKind.Number:
                Assert.Equal(expectedValue.GetValue<decimal>(), actual!.GetValue<decimal>());
                break;
            case JsonValue expectedValue when expectedValue.GetValueKind() == JsonValueKind.String
                && DateTimeOffset.TryParse(expectedValue.GetValue<string>(), out var expectedTime)
                && expectedValue.GetValue<string>().Contains('T', StringComparison.Ordinal):
                Assert.Equal(expectedTime, DateTimeOffset.Parse(actual!.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture));
                Assert.Equal(expectedTime.Offset, DateTimeOffset.Parse(actual!.GetValue<string>(), System.Globalization.CultureInfo.InvariantCulture).Offset);
                break;
            default:
                Assert.True(JsonNode.DeepEquals(expected, actual), $"{path}: expected {expected?.ToJsonString() ?? "null"}, got {actual?.ToJsonString() ?? "null"}.");
                break;
        }
    }
}
