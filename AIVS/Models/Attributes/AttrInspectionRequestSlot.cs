namespace AIVS.Models.Attributes
{
    public class AttrInspectionRequestSlot
    {
        public long Id { get; set; }

        public long InspectionRequestId { get; set; }

        public long Attr_ID { get; set; }

        public string? Attr_No { get; set; }

        public int SlotNo { get; set; }

        public DateTime ProposedDateTime { get; set; }

        public string SlotStatus { get; set; } = "Offered";

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public AttrInspectionRequest? InspectionRequest { get; set; }
    }
}
