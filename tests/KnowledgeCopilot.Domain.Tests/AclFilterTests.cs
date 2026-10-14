using KnowledgeCopilot.Domain.Security;

namespace KnowledgeCopilot.Domain.Tests;

public sealed class AclFilterTests
{
    [Fact]
    public void For_DuplicateGroups_DeduplicatesPrincipals()
    {
        var filter = AclFilter.For(new Principal("t1", "alice", ["hr", "hr", "eng"], []));

        Assert.Equal(["g:eng", "g:hr", "t:t1", "u:alice"], filter.Principals);
    }

    [Fact]
    public void For_UnsortedGroups_ReturnsOrdinallySortedPrincipals()
    {
        var filter = AclFilter.For(new Principal("t1", "bob", ["zeta", "Alpha", "beta"], []));

        Assert.Equal(filter.Principals.Order(StringComparer.Ordinal), filter.Principals);
        Assert.Equal(["g:Alpha", "g:beta", "g:zeta", "t:t1", "u:bob"], filter.Principals);
    }

    [Fact]
    public void For_NoGroups_ContainsUserAndTenant()
    {
        var filter = AclFilter.For(new Principal("t1", "alice", [], []));

        Assert.Equal("t1", filter.TenantId);
        Assert.Equal(["t:t1", "u:alice"], filter.Principals);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void For_EmptyTenant_Throws(string tenantId)
    {
        Assert.Throws<ArgumentException>(() => AclFilter.For(new Principal(tenantId, "alice", [], [])));
    }

    [Fact]
    public void For_EmptyUser_Throws()
    {
        Assert.Throws<ArgumentException>(() => AclFilter.For(new Principal("t1", "", [], [])));
    }

    [Fact]
    public void For_EmptyGroup_Throws()
    {
        Assert.Throws<ArgumentException>(() => AclFilter.For(new Principal("t1", "alice", ["hr", " "], [])));
    }

    [Fact]
    public void For_NullPrincipal_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AclFilter.For(null!));
    }

    [Fact]
    public void Allows_SameTenantAndSharedGroup_ReturnsTrue()
    {
        var filter = AclFilter.For(new Principal("t1", "alice", ["hr"], []));

        Assert.True(filter.Allows("t1", ["g:hr", "g:finance"]));
    }

    [Fact]
    public void Allows_OtherTenantEvenWithSharedPrincipal_ReturnsFalse()
    {
        var filter = AclFilter.For(new Principal("t1", "alice", ["hr"], []));

        Assert.False(filter.Allows("t2", ["g:hr", "u:alice"]));
    }

    [Fact]
    public void Allows_NoSharedPrincipal_ReturnsFalse()
    {
        var filter = AclFilter.For(new Principal("t1", "alice", ["hr"], []));

        Assert.False(filter.Allows("t1", ["g:finance", "u:bob"]));
    }

    [Fact]
    public void Allows_EmptyAcl_ReturnsFalse()
    {
        var filter = AclFilter.For(new Principal("t1", "alice", ["hr"], []));

        Assert.False(filter.Allows("t1", []));
    }

    [Fact]
    public void Allows_PrincipalCaseDiffers_ReturnsFalse()
    {
        var filter = AclFilter.For(new Principal("t1", "alice", ["hr"], []));

        Assert.False(filter.Allows("t1", ["g:HR"]));
    }
}
