namespace KnowledgeCopilot.Domain.Security;

/// <summary>Builds the opaque principal strings stored in <c>acl_principals</c> (D-14).</summary>
public static class AclPrincipal
{
    public const string UserPrefix = "u:";
    public const string GroupPrefix = "g:";
    public const string TenantPrefix = "t:";

    public static string User(string userId) => UserPrefix + userId;

    public static string Group(string groupId) => GroupPrefix + groupId;

    /// <summary>Grants the whole tenant.</summary>
    public static string Tenant(string tenantId) => TenantPrefix + tenantId;
}
