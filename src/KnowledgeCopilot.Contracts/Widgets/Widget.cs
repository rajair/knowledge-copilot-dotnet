using System.Text.Json.Serialization;

namespace KnowledgeCopilot.Contracts.Widgets;

/// <summary>
/// A structured UI element the model produces through a tool call. The <c>type</c> property is the
/// discriminator and is always present on the wire.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(FinancialChartWidget), "financialChart")]
[JsonDerivedType(typeof(ComparisonTableWidget), "comparisonTable")]
[JsonDerivedType(typeof(ActionProposalWidget), "actionProposal")]
public abstract record Widget;
