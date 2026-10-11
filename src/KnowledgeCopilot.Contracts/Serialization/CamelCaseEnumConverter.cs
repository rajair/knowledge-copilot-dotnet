using System.Text.Json;
using System.Text.Json.Serialization;

namespace KnowledgeCopilot.Contracts.Serialization;

/// <summary>
/// Serializes an enum as a camelCase string. Source-generated contexts have no enum naming policy,
/// so each contract enum opts in with <c>[JsonConverter(typeof(CamelCaseEnumConverter&lt;T&gt;))]</c>.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
public sealed class CamelCaseEnumConverter<TEnum>()
    : JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    where TEnum : struct, Enum;
