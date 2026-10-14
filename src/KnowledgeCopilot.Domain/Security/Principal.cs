namespace KnowledgeCopilot.Domain.Security;

/// <summary>
/// The authenticated caller as the core sees it. Always comes from <see cref="Ports.ICurrentPrincipal"/>,
/// never from a request body.
/// </summary>
/// <param name="TenantId">Tenant the caller belongs to; mandatory for every index query.</param>
/// <param name="UserId">Stable user id from the identity provider.</param>
/// <param name="Groups">Opaque group ids (Keycloak group ids or Entra object ids, D-14).</param>
/// <param name="Roles">Application roles; not part of the ACL principal set.</param>
public sealed record Principal(string TenantId, string UserId, IReadOnlyList<string> Groups, IReadOnlyList<string> Roles);
