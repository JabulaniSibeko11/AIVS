using AIVS.Data;
using AIVS.Models.Attributes;
using AIVS.Models.ViewModels.InspectionCalendar;
using AIVS.Models.ViewModels.UserManagement;
using AIVS.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace AIVS.Services.Implementations
{
    public class InspectionCalendarService : IInspectionCalendarService
    {
        private static readonly TimeSpan WorkingStart = new(8, 0, 0);
        private static readonly TimeSpan WorkingEnd = new(16, 0, 0);
        private const int SlotMinutes = 60;
        private readonly AttributesDbContext _context;

        public InspectionCalendarService(AttributesDbContext context) => _context = context;

        public async Task<InspectionCalendarPageVm> GetMonthAsync(AivsCurrentUserVm currentUser, int year, int month)
        {
            var userId = RequireUserId(currentUser);
            if (month < 1 || month > 12) throw new InvalidOperationException("Invalid calendar month.");

            var monthStart = new DateTime(year, month, 1);
            var monthEndExclusive = monthStart.AddMonths(1);
            var gridStart = StartOfWeekMonday(monthStart);
            var gridEndExclusive = StartOfWeekMonday(monthEndExclusive.AddDays(6));

            var blocks = await _context.AttrInspectionCalendarBlocks.AsNoTracking()
                .Where(x => x.UserId == userId && x.IsActive && x.BlockedFrom < gridEndExclusive && x.BlockedTo > gridStart)
                .OrderBy(x => x.BlockedFrom).ToListAsync();

            var bookings = await _context.AttrInspectionRequests.AsNoTracking()
                .Where(x => x.RequestedByUserId == userId && x.ConfirmedDateTime != null &&
                            x.Status != "Expired" && x.Status != "Cancelled" &&
                            x.ConfirmedDateTime >= gridStart && x.ConfirmedDateTime < gridEndExclusive)
                .Select(x => new { x.Id, x.Attr_No, Start = x.ConfirmedDateTime!.Value }).ToListAsync();

            var vm = new InspectionCalendarPageVm
            {
                UserId = userId,
                UserDisplayName = currentUser.FullName ?? currentUser.Username ?? currentUser.WindowsUsername ?? "Valuer",
                Year = year,
                Month = month,
                MonthStart = monthStart,
                MonthEndExclusive = monthEndExclusive,
                DefaultWorkingDayStart = WorkingStart,
                DefaultWorkingDayEnd = WorkingEnd,
                SlotMinutes = SlotMinutes
            };

            for (var date = gridStart.Date; date < gridEndExclusive.Date; date = date.AddDays(1))
            {
                var dayBlocks = blocks.Where(x => x.BlockedFrom < date.AddDays(1) && x.BlockedTo > date).ToList();
                var dayBookings = bookings.Where(x => x.Start.Date == date).ToList();
                var dayVm = new InspectionCalendarDayVm
                {
                    Date = date,
                    IsCurrentMonth = date.Month == month && date.Year == year,
                    IsPast = date.Date < DateTime.Today,
                    IsWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                    IsWholeDayBlocked = dayBlocks.Any(x => x.IsWholeDay),
                    Blocks = dayBlocks.Select(x => new InspectionCalendarBlockVm
                    {
                        Id = x.Id, BlockedFrom = x.BlockedFrom, BlockedTo = x.BlockedTo,
                        IsWholeDay = x.IsWholeDay, Reason = x.Reason
                    }).ToList(),
                    Bookings = dayBookings.Select(x => new InspectionCalendarBookingVm
                    {
                        InspectionRequestId = x.Id, AttrNo = x.Attr_No,
                        Start = x.Start, End = x.Start.AddMinutes(SlotMinutes)
                    }).ToList()
                };
                dayVm.Slots = BuildSlots(date, dayBlocks, dayVm.Bookings);
                vm.Days.Add(dayVm);
            }
            return vm;
        }

        public async Task SetDayAvailabilityAsync(AivsCurrentUserVm currentUser, SetInspectionDayAvailabilityVm vm)
        {
            var userId = RequireUserId(currentUser);
            var date = vm.Date.Date;
            if (date < DateTime.Today) throw new InvalidOperationException("Past dates cannot be changed.");
            var username = currentUser.Username ?? currentUser.WindowsUsername ?? userId.ToString();

            var existing = await _context.AttrInspectionCalendarBlocks
                .Where(x => x.UserId == userId && x.IsActive && x.BlockedFrom < date.AddDays(1) && x.BlockedTo > date)
                .ToListAsync();
            foreach (var row in existing)
            {
                row.IsActive = false; row.UpdatedBy = username; row.UpdatedDate = DateTime.Now;
            }

            if (vm.AvailableFrom == null || vm.AvailableTo == null)
            {
                AddBlock(userId, date, date.AddDays(1), true, vm.Reason ?? "Whole day unavailable", username);
            }
            else
            {
                if (vm.AvailableFrom < WorkingStart || vm.AvailableTo > WorkingEnd || vm.AvailableTo <= vm.AvailableFrom)
                    throw new InvalidOperationException($"Availability must be between {WorkingStart:hh\\:mm} and {WorkingEnd:hh\\:mm}.");

                var availableStart = date.Add(vm.AvailableFrom.Value);
                var availableEnd = date.Add(vm.AvailableTo.Value);
                if (availableStart > date.Add(WorkingStart))
                    AddBlock(userId, date.Add(WorkingStart), availableStart, false, vm.Reason ?? "Unavailable before selected inspection hours", username);
                if (availableEnd < date.Add(WorkingEnd))
                    AddBlock(userId, availableEnd, date.Add(WorkingEnd), false, vm.Reason ?? "Unavailable after selected inspection hours", username);
            }
            await _context.SaveChangesAsync();
        }

        public async Task BlockPeriodAsync(AivsCurrentUserVm currentUser, BlockInspectionPeriodVm vm)
        {
            var userId = RequireUserId(currentUser);
            if (vm.BlockedTo <= vm.BlockedFrom) throw new InvalidOperationException("Blocked end time must be after the start time.");
            if (vm.BlockedFrom.Date < DateTime.Today) throw new InvalidOperationException("Past dates cannot be blocked.");
            var username = currentUser.Username ?? currentUser.WindowsUsername ?? userId.ToString();
            AddBlock(userId, vm.BlockedFrom, vm.BlockedTo,
                vm.BlockedFrom.TimeOfDay == TimeSpan.Zero && vm.BlockedTo == vm.BlockedFrom.Date.AddDays(1),
                vm.Reason, username);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveBlockAsync(AivsCurrentUserVm currentUser, long blockId)
        {
            var userId = RequireUserId(currentUser);
            var row = await _context.AttrInspectionCalendarBlocks.FirstOrDefaultAsync(x => x.Id == blockId && x.UserId == userId && x.IsActive)
                ?? throw new InvalidOperationException("Calendar block could not be found.");
            row.IsActive = false;
            row.UpdatedBy = currentUser.Username ?? currentUser.WindowsUsername ?? userId.ToString();
            row.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasAnyAvailableSlotAsync(int userId, DateTime from, DateTime to)
            => (await GetAvailableSlotsAsync(userId, from, to)).Any();

        public async Task<List<InspectionCalendarSlotVm>> GetAvailableSlotsAsync(int userId, DateTime from, DateTime to)
        {
            if (to <= from) return new();
            var blocks = await _context.AttrInspectionCalendarBlocks.AsNoTracking()
                .Where(x => x.UserId == userId && x.IsActive && x.BlockedFrom < to && x.BlockedTo > from).ToListAsync();
            var bookings = await _context.AttrInspectionRequests.AsNoTracking()
                .Where(x => x.RequestedByUserId == userId && x.ConfirmedDateTime != null &&
                            x.Status != "Expired" && x.Status != "Cancelled" &&
                            x.ConfirmedDateTime < to && x.ConfirmedDateTime >= from.AddMinutes(-SlotMinutes))
                .Select(x => x.ConfirmedDateTime!.Value).ToListAsync();

            var result = new List<InspectionCalendarSlotVm>();
            for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                for (var start = date.Add(WorkingStart); start.AddMinutes(SlotMinutes) <= date.Add(WorkingEnd); start = start.AddMinutes(SlotMinutes))
                {
                    var end = start.AddMinutes(SlotMinutes);
                    if (start < from || start >= to) continue;
                    var blocked = blocks.Any(x => x.BlockedFrom < end && x.BlockedTo > start);
                    var booked = bookings.Any(x => x < end && x.AddMinutes(SlotMinutes) > start);
                    if (!blocked && !booked)
                        result.Add(new InspectionCalendarSlotVm { Start = start, End = end, IsAvailable = true, Status = "Available" });
                }
            }
            return result;
        }

        private List<InspectionCalendarSlotVm> BuildSlots(DateTime date, List<AttrInspectionCalendarBlock> blocks, List<InspectionCalendarBookingVm> bookings)
        {
            var slots = new List<InspectionCalendarSlotVm>();
            if (date.Date < DateTime.Today || date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return slots;
            for (var start = date.Add(WorkingStart); start.AddMinutes(SlotMinutes) <= date.Add(WorkingEnd); start = start.AddMinutes(SlotMinutes))
            {
                var end = start.AddMinutes(SlotMinutes);
                var blocked = blocks.Any(x => x.BlockedFrom < end && x.BlockedTo > start);
                var booked = bookings.Any(x => x.Start < end && x.End > start);
                slots.Add(new InspectionCalendarSlotVm
                {
                    Start = start, End = end, IsAvailable = !blocked && !booked,
                    Status = booked ? "Booked" : blocked ? "Blocked" : "Available"
                });
            }
            return slots;
        }

        private void AddBlock(int userId, DateTime from, DateTime to, bool wholeDay, string? reason, string username)
            => _context.AttrInspectionCalendarBlocks.Add(new AttrInspectionCalendarBlock
            {
                UserId = userId, BlockedFrom = from, BlockedTo = to, IsWholeDay = wholeDay,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(), IsActive = true,
                CreatedBy = username, CreatedDate = DateTime.Now
            });

        private static int RequireUserId(AivsCurrentUserVm currentUser)
        {
            if (!currentUser.HasAccess || currentUser.UserId == null) throw new InvalidOperationException("Your AIVS user could not be verified.");
            return currentUser.UserId.Value;
        }

        private static DateTime StartOfWeekMonday(DateTime value)
        {
            var diff = ((int)value.DayOfWeek + 6) % 7;
            return value.Date.AddDays(-diff);
        }
    }
}
