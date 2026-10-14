using KnowledgeCopilot.Domain.Security;

namespace KnowledgeCopilot.Domain.Ports;

/// <summary>
/// The caller of the current request, resolved from validated identity on the server; never from the
/// request body. Implementations supply <see cref="Principal"/>; the other members derive from it so the
/// ACL principal set is built in one place (<see cref="AclFilter"/>).
/// </summary>
public interface ICurrentPrincipal
{
    Principal Principal { get; }

    string TenantId => Principal.TenantId;

    string UserId => Principal.UserId;

    IReadOnlyList<string> Groups => Principal.Groups;

    /// <summary><c>u:{userId}</c>, <c>g:{groupId}</c>… and <c>t:{tenantId}</c>.</summary>
    IReadOnlyList<string> ToAclPrincipals() => AclFilter.For(Principal).Principals;
}
