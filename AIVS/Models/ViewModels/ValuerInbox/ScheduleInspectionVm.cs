namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class ScheduleInspectionVm
    {
        public long AttrId { get; set; }

        public long ReviewId { get; set; }

        public DateTime Option1 { get; set; }

        public DateTime Option2 { get; set; }

        public DateTime Option3 { get; set; }

        public string? RequestComment { get; set; }
    }
}
