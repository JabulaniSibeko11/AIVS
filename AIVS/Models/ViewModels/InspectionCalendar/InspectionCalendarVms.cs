namespace AIVS.Models.ViewModels.InspectionCalendar
{
    public class InspectionCalendarPageVm
    {
        public int UserId { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime MonthStart { get; set; }
        public DateTime MonthEndExclusive { get; set; }
        public TimeSpan DefaultWorkingDayStart { get; set; } = new(8,0,0);
        public TimeSpan DefaultWorkingDayEnd { get; set; } = new(16,0,0);
        public int SlotMinutes { get; set; } = 60;
        public List<InspectionCalendarDayVm> Days { get; set; } = new();
        public string MonthLabel => MonthStart.ToString("MMMM yyyy");
    }

    public class InspectionCalendarDayVm
    {
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsPast { get; set; }
        public bool IsWeekend { get; set; }
        public bool IsWholeDayBlocked { get; set; }
        public List<InspectionCalendarBlockVm> Blocks { get; set; } = new();
        public List<InspectionCalendarBookingVm> Bookings { get; set; } = new();
        public List<InspectionCalendarSlotVm> Slots { get; set; } = new();
        public bool HasAnyAvailability => Slots.Any(x => x.IsAvailable);
    }

    public class InspectionCalendarBlockVm
    {
        public long Id { get; set; }
        public DateTime BlockedFrom { get; set; }
        public DateTime BlockedTo { get; set; }
        public bool IsWholeDay { get; set; }
        public string? Reason { get; set; }
    }

    public class InspectionCalendarBookingVm
    {
        public long InspectionRequestId { get; set; }
        public string? AttrNo { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class InspectionCalendarSlotVm
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsAvailable { get; set; }
        public string Status { get; set; } = "Available";
    }

    public class SetInspectionDayAvailabilityVm
    {
        public DateTime Date { get; set; }
        public TimeSpan? AvailableFrom { get; set; }
        public TimeSpan? AvailableTo { get; set; }
        public string? Reason { get; set; }
    }

    public class BlockInspectionPeriodVm
    {
        public DateTime BlockedFrom { get; set; }
        public DateTime BlockedTo { get; set; }
        public string? Reason { get; set; }
    }
}
