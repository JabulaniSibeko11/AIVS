using AIVS.Models.ViewModels.InspectionCalendar;
using AIVS.Models.ViewModels.UserManagement;

namespace AIVS.Services.Interface
{
    public interface IInspectionCalendarService
    {
        Task<InspectionCalendarPageVm> GetMonthAsync(AivsCurrentUserVm currentUser, int year, int month);
        Task SetDayAvailabilityAsync(AivsCurrentUserVm currentUser, SetInspectionDayAvailabilityVm vm);
        Task BlockPeriodAsync(AivsCurrentUserVm currentUser, BlockInspectionPeriodVm vm);
        Task RemoveBlockAsync(AivsCurrentUserVm currentUser, long blockId);
        Task<bool> HasAnyAvailableSlotAsync(int userId, DateTime from, DateTime to);
        Task<List<InspectionCalendarSlotVm>> GetAvailableSlotsAsync(int userId, DateTime from, DateTime to);
    }
}
