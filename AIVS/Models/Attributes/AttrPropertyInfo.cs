using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_Property_Info")]
    public class AttrPropertyInfo
    {
        [Key]
        public long Attr_ID { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [StringLength(100)]
        public string? Attr_No { get; set; }

        public int? Attr_PropertyDetailsId { get; set; }

        [StringLength(100)]
        public string? Objector_Type { get; set; }

        [StringLength(100)]
        public string? Property_Type { get; set; }

        [StringLength(255)]
        public string? Property_Desc { get; set; }

        [StringLength(100)]
        public string? Premise_id { get; set; }

        [StringLength(100)]
        public string? Unit_key { get; set; }

        [StringLength(100)]
        public string? Property_id { get; set; }

        [StringLength(100)]
        public string? Valuation_Key { get; set; }

        [StringLength(100)]
        public string? Sector { get; set; }

        [StringLength(50)]
        public string? RollType { get; set; }

        [StringLength(255)]
        public string? RollDescription { get; set; }

        [StringLength(100)]
        public string? SubmittedByUserId { get; set; }

        // SAP number of the Genesis administrator who captured the submission
        // on behalf of the client. Null means the client submitted directly.
        [StringLength(100)]
        public string? Capturer { get; set; }

        [StringLength(255)]
        public string? SubmittedByName { get; set; }

        [StringLength(255)]
        public string? SubmittedByEmail { get; set; }

        [StringLength(50)]
        public string? SubmittedByPhone { get; set; }

        [StringLength(50)]
        public string? SubmissionSource { get; set; }

        public DateTime SubmissionDateTime { get; set; } = DateTime.Now;

        public string? ClientComment { get; set; }

        public string? ClientEvidencePath { get; set; }

        [Required]
        [StringLength(50)]
        public string Attr_Status { get; set; } = "Submitted";

        public bool IsActive { get; set; } = true;

        [StringLength(255)]
        public string? Task_Assigner { get; set; }

        [StringLength(1000)]
        public string? TaskAssignerComment { get; set; }

        [StringLength(255)]
        public string? Task_Assigned_To { get; set; }

        [StringLength(100)]
        public string? Task_Assigned_To_UserId { get; set; }

        public DateTime? Task_Assigned_DateTime { get; set; }

        [StringLength(255)]
        public string? Valuer { get; set; }

        [StringLength(100)]
        public string? ValuerUserId { get; set; }

        [StringLength(2000)]
        public string? ValuerComment { get; set; }

        public string? ValuerEvidencePath { get; set; }

        [StringLength(50)]
        public string? ValuerDecision { get; set; }

        [StringLength(2000)]
        public string? RejectionReason { get; set; }

        public DateTime? ValuerDecisionDateTime { get; set; }

        [StringLength(255)]
        public string? Approver { get; set; }

        [StringLength(100)]
        public string? ApproverUserId { get; set; }

        [StringLength(2000)]
        public string? ApproverComment { get; set; }

        public string? ApproverEvidencePath { get; set; }

        [StringLength(50)]
        public string? ApproverDecision { get; set; }

        public DateTime? ApproverDecisionDateTime { get; set; }

        public bool Physical_Inspection_Required { get; set; }

        [StringLength(50)]
        public string? Physical_Inspection_Status { get; set; }

        [StringLength(2000)]
        public string? Physical_Inspection_Comment { get; set; }

        public DateTime? Inspection_Scheduled_Date { get; set; }

        public TimeSpan? Inspection_Scheduled_Time { get; set; }

        [StringLength(500)]
        public string? Inspection_Address { get; set; }

        [StringLength(255)]
        public string? Inspection_Valuer { get; set; }

        [StringLength(100)]
        public string? Inspection_ValuerUserId { get; set; }

        [StringLength(100)]
        public string? Digital_Valuer_ID { get; set; }

        public DateTime? Digital_Valuer_ID_GeneratedDateTime { get; set; }

        [StringLength(100)]
        public string? Inspection_Outcome { get; set; }

        [StringLength(2000)]
        public string? Inspection_Outcome_Comment { get; set; }

        public string? Inspection_EvidencePath { get; set; }

        public bool RevisionRequired { get; set; }

        [StringLength(255)]
        public string? RevisionRequestedBy { get; set; }

        public DateTime? RevisionRequestedDateTime { get; set; }

        [StringLength(2000)]
        public string? RevisionReason { get; set; }

        [StringLength(255)]
        public string? RevisedBy { get; set; }

        public DateTime? RevisedDateTime { get; set; }

        [StringLength(2000)]
        public string? RevisionComment { get; set; }

        public bool ReadyForOvvioExtract { get; set; }

        [StringLength(50)]
        public string? OvvioExtractStatus { get; set; }

        [StringLength(100)]
        public string? OvvioExtractBatchNo { get; set; }

        public DateTime? OvvioExtractDateTime { get; set; }

        [StringLength(255)]
        public string? OvvioExtractedBy { get; set; }

        public string? OvvioExtractError { get; set; }

        public bool IsWithdrawn { get; set; }

        public DateTime? WithdrawnDateTime { get; set; }

        [StringLength(100)]
        public string? WithdrawnByUserId { get; set; }

        [StringLength(255)]
        public string? WithdrawnByName { get; set; }

        [StringLength(1000)]
        public string? WithdrawalReason { get; set; }

        public int Evidence_Count { get; set; }

        public bool Has_Client_Evidence { get; set; }

        public DateTime? Last_Evidence_Uploaded_DateTime { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public string? RoutedSector { get; set; }

        public DateTime? EvidenceLockedDateTime { get; set; }
        public DateTime? RoutedToSectorDateTime { get; set; }
        public string? RoutingError { get; set; }
        public string? SectorManagerQaDecision { get; set; }
        public string? SectorManagerQaComment { get; set; }
        public string? SectorManagerQaBy { get; set; }
        public string? SectorManagerQaUserId { get; set; }
        public DateTime? SectorManagerQaDateTime { get; set; }


        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [ForeignKey(nameof(Attr_PropertyDetailsId))]
        public AttrPropertyDetails? PropertyDetails { get; set; }

        public ICollection<AttrPropertyInfoAuditTrail> AuditTrails { get; set; } = new List<AttrPropertyInfoAuditTrail>();
        public ICollection<AttrWithdrawals> Withdrawals { get; set; } = new List<AttrWithdrawals>();
        public ICollection<AttrFiles> Files { get; set; } = new List<AttrFiles>();
    }
}
