namespace AIVS.Models.Attributes
{
    public class AttrInspectionRequest
    {
        public long Id { get; set; }

        public long Attr_ID { get; set; }

        public string? Attr_No { get; set; }

        public long? ReviewId { get; set; }

        public int RequestedByUserId { get; set; }

        public string? RequestedByUsername { get; set; }

        public string? RequestedByName { get; set; }

        public string? RequestedByEmail { get; set; }

        public string? ClientName { get; set; }

        public string? ClientEmail { get; set; }

        public string? ClientCellNo { get; set; }

        public string Status { get; set; } = "PendingClientResponse";

        public string? ClientResponseChannel { get; set; }

        public string? ClientResponseComment { get; set; }

        public DateTime? ClientRespondedAt { get; set; }

        public long? ConfirmedSlotId { get; set; }

        public DateTime? ConfirmedDateTime { get; set; }

        public string? RequestComment { get; set; }

        public Guid EmailToken { get; set; } = Guid.NewGuid();

        public DateTime? EmailTokenExpiresAt { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string? InspectionPin { get; set; }

        public DateTime? InspectionPinGeneratedAt { get; set; }

        public bool ValuerDetailsSent { get; set; }

        public DateTime? ValuerDetailsSentAt { get; set; }

        public int? ValuerDetailsSentByUserId { get; set; }

        public string? ValuerDetailsSentByName { get; set; }
        public string? ValuerSapNumber { get; set; }

      

        public DateTime? ExpiredAt { get; set; }

        public string? ExpiryReason { get; set; }

        public DateTime? PinVerifiedAt { get; set; }

        public string? PinVerifiedByEmail { get; set; }

        public int PinFailedAttempts { get; set; }

        public DateTime? PinValidFrom { get; set; }

        public DateTime? PinValidUntil { get; set; }

        public DateTime? PinUsedAt { get; set; }

        public string? PinUsedByEmail { get; set; }

        public string? PinUsedIpAddress { get; set; }

        public string? PinUsedUserAgent { get; set; }
        public List<AttrInspectionRequestSlot> Slots { get; set; } = new();

        
    }
}