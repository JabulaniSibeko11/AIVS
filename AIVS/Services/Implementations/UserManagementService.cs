using AIVS.Data;
using AIVS.Models.Configuration;
using AIVS.Models.UserManagement;
using AIVS.Models.ViewModels.SectorInbox;
using AIVS.Models.ViewModels.UserManagement;
using AIVS.Services.Interface;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Security.Claims;

namespace AIVS.Services.Implementations
{
    public class UserManagementService : IUserManagementService
    {
        private readonly string _connString;
        private readonly AivsSettings _settings;
        private readonly ILogger<UserManagementService> _logger;
        private readonly string _attributesConnString;
        public UserManagementService(
     IConfiguration config,
     IOptions<AivsSettings> settings,
     ILogger<UserManagementService> logger)
        {
            _connString = config.GetConnectionString("UserManagementConnection")
                ?? throw new InvalidOperationException("UserManagementConnection is missing.");

            _attributesConnString = config.GetConnectionString("AttributesConnection")
                ?? throw new InvalidOperationException("AttributesConnection is missing.");

            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<UserManagementResult?> ValidateAdminAsync(string sapNumber)
        {
            if (string.IsNullOrWhiteSpace(sapNumber))
                return null;

            var username = $@"{_settings.SapDomain}\{sapNumber.Trim()}";

            return await CallLoginSpAsync(username);
        }

        public async Task<UserManagementResult?> ValidateByWindowsIdentityAsync(string windowsIdentityName)
        {
            if (string.IsNullOrWhiteSpace(windowsIdentityName))
                return null;

            return await CallLoginSpAsync(windowsIdentityName.Trim());
        }

        public async Task<AivsCurrentUserVm> GetCurrentUserAsync(ClaimsPrincipal user)
        {
            var windowsUsername = user.Identity?.Name;

            if (string.IsNullOrWhiteSpace(windowsUsername))
            {
                return new AivsCurrentUserVm
                {
                    HasAccess = false,
                    AccessMessage = "Windows username could not be detected."
                };
            }

            try
            {
                var result = await ValidateByWindowsIdentityAsync(windowsUsername);

                if (result == null)
                {
                    return new AivsCurrentUserVm
                    {
                        HasAccess = false,
                        WindowsUsername = windowsUsername,
                        AccessMessage = $"UserManagement login failed for {windowsUsername}. Check dbo.Login and SystemID {_settings.SystemId}."
                    };
                }

                return new AivsCurrentUserVm
                {
                    HasAccess = true,
                    WindowsUsername = windowsUsername,

                    UserId = result.UserID,
                    Username = result.Username?.Trim(),

                    FullName = string.IsNullOrWhiteSpace(result.FullName)
         ? result.Username?.Trim()
         : result.FullName,

                    Email = result.EmailAddress?.Trim(),

                    // This must stay as the UserManagement role.
                    Role = result.Role?.Trim(),

                    // This is only for layout display.
                    Position = result.Position?.Trim(),

                    AccessMessage = "Access granted."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AIVS failed while calling UserManagement dbo.Login for {WindowsUsername}",
                    windowsUsername);

                return new AivsCurrentUserVm
                {
                    HasAccess = false,
                    WindowsUsername = windowsUsername,
                    AccessMessage = "AIVS could not verify your UserManagement access using dbo.Login."
                };
            }
        }

        private async Task<UserManagementResult?> CallLoginSpAsync(string username)
        {
            await using var conn = new SqlConnection(_connString);

            var result = await conn.QueryFirstOrDefaultAsync<UserManagementResult>(
                "dbo.Login",
                new
                {
                    Username = username,
                    System = _settings.SystemId
                },
                commandType: CommandType.StoredProcedure);

            return result;
        }
        public async Task<List<SectorValuerVm>> GetValuersAsync(string? sector)
        {
            await using var conn = new SqlConnection(_attributesConnString);

            var result = await conn.QueryAsync<SectorValuerVm>(
                "[dbo].[AIVS_GetActiveValuers]",
                new
                {
                    SystemId = _settings.SystemId
                },
                commandType: CommandType.StoredProcedure);

            return result.ToList();
        }
    }
}