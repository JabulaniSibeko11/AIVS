using AIVS.Models.ViewModels.SectorInbox;

namespace AIVS.Services.Interface
{
    public interface ISectorInboxService
    {
        Task<SectorInboxPageVm> BuildSectorInboxAsync(string? selectedSector);

        Task<List<SectorInboxTileVm>> GetSectorTilesAsync();

        Task<List<SectorInboxItemVm>> GetSectorItemsAsync(string sector);

        Task<SectorAssignmentResultVm> AssignSelectedToMeAsync(
            List<long> attrIds,
            int userId,
            string username,
            string fullName,
            string? email,
            string? role);

        Task<SectorAssignmentResultVm> AssignSelectedToValuerAsync(
    List<long> attrIds,
    int valuerUserId,
    string valuerUsername,
    string valuerFullName,
    string? valuerEmail,
    string? valuerRole,
    int managerUserId,
    string managerUsername,
    string managerFullName,
    string? managerRole);
    }
}
