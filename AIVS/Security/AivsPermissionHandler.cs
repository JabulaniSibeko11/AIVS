using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;

namespace AIVS.Security;

public sealed class AivsPermissionHandler : AuthorizationHandler<AivsPermissionRequirement>
{
    private readonly IUserManagementService _users;
    private readonly IAivsRoleAccessService _access;

    public AivsPermissionHandler(IUserManagementService users, IAivsRoleAccessService access)
    {
        _users = users;
        _access = access;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AivsPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;
        var currentUser = await _users.GetCurrentUserAsync(context.User);
        if (_access.HasPermission(currentUser, requirement.Permission)) context.Succeed(requirement);
    }
}
