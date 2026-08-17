using AIVS.Models.ViewModels.UserManagement;
using AIVS.Models.Configuration;
using Microsoft.Extensions.Options;
using AIVS.Security;
using AIVS.Services.Interface;

namespace AIVS.Services.Implementations;

public sealed class AivsRoleAccessService : IAivsRoleAccessService
{
    private readonly DemoQaSettings _demoQa;

    public AivsRoleAccessService(IOptions<DemoQaSettings> demoQa)
    {
        _demoQa = demoQa.Value;
    }
    private static readonly HashSet<string> ValuerRoles = new(StringComparer.OrdinalIgnoreCase) { "VALUER" };
    private static readonly HashSet<string> SectorManagerRoles = new(StringComparer.OrdinalIgnoreCase) { "SECTOR MANAGER", "AREA MANAGER", "OPERATIONAL MANAGER" };
    private static readonly HashSet<string> SeniorManagerRoles = new(StringComparer.OrdinalIgnoreCase) { "SENIOR MANAGER" };
    private static readonly HashSet<string> DeputyDirectorRoles = new(StringComparer.OrdinalIgnoreCase) { "DEPUTY DIRECTOR" };
    private static readonly HashSet<string> ExecutiveRoles = new(StringComparer.OrdinalIgnoreCase) { "EXECUTIVE" };
    private static readonly HashSet<string> AdministratorRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "SYSTEM ADMIN", "VALUATION ADMIN", "ADMIN", "ADMINISTRATOR", "IT MANAGER"
    };

    public string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return string.Empty;
        return string.Join(' ', role.Replace('\u00A0', ' ').Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    }

    public bool IsValuer(AivsCurrentUserVm? user) => IsIn(user, ValuerRoles);
    public bool IsSectorManager(AivsCurrentUserVm? user) => IsIn(user, SectorManagerRoles);
    public bool IsSeniorManager(AivsCurrentUserVm? user) => IsIn(user, SeniorManagerRoles);
    public bool IsLeadership(AivsCurrentUserVm? user) => IsIn(user, DeputyDirectorRoles) || IsIn(user, ExecutiveRoles);
    public bool IsAdministrator(AivsCurrentUserVm? user) => IsIn(user, AdministratorRoles);

    public bool HasPermission(AivsCurrentUserVm? user, AivsPermission permission)
    {
        if (user?.HasAccess != true || user.UserId == null) return false;

        if (IsDemoQaUser(user) && _demoQa.AllowTestUserAllQaRoles)
        {
            if (permission is AivsPermission.AccessAivs
                or AivsPermission.ViewSectorInbox
                or AivsPermission.SelfAssign
                or AivsPermission.AssignWork
                or AivsPermission.ReviewSubmission
                or AivsPermission.PerformSectorManagerQa
                or AivsPermission.PerformSeniorManagerQa
                or AivsPermission.ViewSectorStatistics)
                return true;
        }

        if (IsAdministrator(user)) return true;

        var valuer = IsValuer(user);
        var sectorManager = IsSectorManager(user);
        var seniorManager = IsSeniorManager(user);
        var leadership = IsLeadership(user);

        return permission switch
        {
            AivsPermission.AccessAivs => valuer || sectorManager || seniorManager || leadership,
            AivsPermission.ViewSectorInbox => valuer || sectorManager || seniorManager,
            AivsPermission.SelfAssign => valuer || sectorManager || seniorManager,
            AivsPermission.AssignWork => sectorManager || seniorManager,
            AivsPermission.ReviewSubmission => valuer || sectorManager || seniorManager,
            AivsPermission.PerformSectorManagerQa => sectorManager,
            AivsPermission.PerformSeniorManagerQa => seniorManager,
            AivsPermission.ViewSectorStatistics => sectorManager || seniorManager,
            AivsPermission.ViewExecutiveStatistics => leadership,
            AivsPermission.ExportStatistics => leadership,
            AivsPermission.ViewAllSectors => leadership,
            AivsPermission.AdministerAivs => false,
            _ => false
        };
    }

    private bool IsDemoQaUser(AivsCurrentUserVm? user)
    {
        if (!_demoQa.Enabled || user == null) return false;
        var expected = (_demoQa.TestUserWindowsUsername ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(expected)) return false;

        return string.Equals(user.WindowsUsername?.Trim(), expected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.Username?.Trim(), expected, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(user.SapNumber) && expected.EndsWith("\\" + user.SapNumber.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private bool IsIn(AivsCurrentUserVm? user, HashSet<string> roles) => roles.Contains(NormalizeRole(user?.Role));
}
