using KnowledgeCopilot.Domain.Ports;
using KnowledgeCopilot.Domain.Security;
using Microsoft.Extensions.Options;

namespace KnowledgeCopilot.Adapters.Fakes;

/// <summary>Groups for the development principal, bound from <c>KnowledgeCopilot:DevPrincipal</c>.</summary>
public sealed class DevPrincipalOptions
{
    public const string SectionName = "KnowledgeCopilot:DevPrincipal";

    public IList<string> Groups { get; } = [];
}

/// <summary>
/// Fixed principal for local development: tenant <c>dev</c>, user <c>dev-user</c>, groups from
/// configuration. Registered only in the Development environment (D-2-01).
/// </summary>
public sealed class DevCurrentPrincipal(IOptions<DevPrincipalOptions> options) : ICurrentPrincipal
{
    public const string DevTenantId = "dev";
    public const string DevUserId = "dev-user";

    public Principal Principal { get; } = new(
        DevTenantId,
        DevUserId,
        [.. options.Value.Groups.Where(g => !string.IsNullOrWhiteSpace(g)).Distinct(StringComparer.Ordinal)],
        []);
}
