namespace KnowledgeCopilot.Domain.Security;

/// <summary>
/// The pre-filter every index query carries (architecture §8): <c>tenant_id == TenantId AND
/// acl_principals ANY OF Principals</c>. Built only from a <see cref="Principal"/>, so callers cannot
/// widen it or drop the tenant. Each index adapter translates it into its engine's filter syntax.
/// </summary>
public sealed class AclFilter
{
    private readonly HashSet<string> _principalSet;

    private AclFilter(string tenantId, IReadOnlyList<string> principals)
    {
        TenantId = tenantId;
        Principals = principals;
        _principalSet = new HashSet<string>(principals, StringComparer.Ordinal);
    }

    /// <summary>Tenant every result must belong to.</summary>
    public string TenantId { get; }

    /// <summary><c>u:{userId}</c>, <c>g:{groupId}</c>… and <c>t:{tenantId}</c>; distinct and ordinally sorted.</summary>
    public IReadOnlyList<string> Principals { get; }

    /// <summary>Builds the filter for <paramref name="principal"/>.</summary>
    /// <exception cref="ArgumentException">The tenant, the user or a group id is empty.</exception>
    public static AclFilter For(Principal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (string.IsNullOrWhiteSpace(principal.TenantId))
        {
            throw new ArgumentException("A principal must have a tenant id.", nameof(principal));
        }

        if (string.IsNullOrWhiteSpace(principal.UserId))
        {
            throw new ArgumentException("A principal must have a user id.", nameof(principal));
        }

        var principals = new SortedSet<string>(StringComparer.Ordinal)
        {
            AclPrincipal.User(principal.UserId),
            AclPrincipal.Tenant(principal.TenantId),
        };

        foreach (var group in principal.Groups)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException("Group ids must not be empty.", nameof(principal));
            }

            principals.Add(AclPrincipal.Group(group));
        }

        return new AclFilter(principal.TenantId, [.. principals]);
    }

    /// <summary>
    /// Evaluates the filter for one stored chunk. For adapters that filter in process (the in-memory
    /// fake); real engines evaluate the translated filter themselves.
    /// </summary>
    public bool Allows(string tenantId, IEnumerable<string> aclPrincipals)
    {
        ArgumentNullException.ThrowIfNull(aclPrincipals);
        return string.Equals(tenantId, TenantId, StringComparison.Ordinal) && aclPrincipals.Any(_principalSet.Contains);
    }
}
