using Microsoft.AspNetCore.Authorization;

namespace AIVS.Security;

public sealed class AivsPermissionRequirement : IAuthorizationRequirement
{
    public AivsPermissionRequirement(AivsPermission permission) => Permission = permission;
    public AivsPermission Permission { get; }
}
