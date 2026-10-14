using KnowledgeCopilot.Domain.Ports;
using KnowledgeCopilot.Domain.Security;

namespace KnowledgeCopilot.Adapters.ContractTests;

/// <summary>Behaviour every <see cref="ICurrentPrincipal"/> must show (step 2 "Port semantics").</summary>
public abstract class CurrentPrincipalContract
{
    /// <summary>Creates a principal that belongs to at least one group.</summary>
    protected abstract ICurrentPrincipal Create();

    [Fact]
    public void ToAclPrincipals_Always_ContainsUserAndTenant()
    {
        var principal = Create();

        var acl = principal.ToAclPrincipals();

        Assert.Contains(AclPrincipal.User(principal.UserId), acl);
        Assert.Contains(AclPrincipal.Tenant(principal.TenantId), acl);
    }

    [Fact]
    public void ToAclPrincipals_Groups_ContainsOneEntryPerGroup()
    {
        var principal = Create();
        Assert.NotEmpty(principal.Groups);

        var acl = principal.ToAclPrincipals();

        Assert.All(principal.Groups, group => Assert.Contains(AclPrincipal.Group(group), acl));
        Assert.Equal(principal.Groups.Distinct(StringComparer.Ordinal).Count() + 2, acl.Count);
    }

    [Fact]
    public void ToAclPrincipals_Always_MatchesAclFilter()
    {
        var principal = Create();

        Assert.Equal(AclFilter.For(principal.Principal).Principals, principal.ToAclPrincipals());
    }

    [Fact]
    public void TenantId_Always_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(Create().TenantId));
    }
}
