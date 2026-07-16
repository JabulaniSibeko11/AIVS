namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class InspectionRequestVm
    {
        public long Id { get; set; }

        public string? Status { get; set; }

        public string? ClientEmail { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ConfirmedDateTime { get; set; }

        public bool ValuerDetailsSent { get; set; }

        public DateTime? ValuerDetailsSentAt { get; set; }

        public string? InspectionPin { get; set; }

        public List<InspectionSlotVm> Slots { get; set; } = new();
    }

    public class InspectionSlotVm
    {
        public long Id { get; set; }

        public int SlotNo { get; set; }

        public DateTime ProposedDateTime { get; set; }

        public string? SlotStatus { get; set; }
    }
}
