using AIVS.Models.Attributes;
using Microsoft.EntityFrameworkCore;


namespace AIVS.Data
{
    public class AttributesDbContext : DbContext
    {
        public AttributesDbContext(DbContextOptions<AttributesDbContext> options)
            : base(options)
        {
        }

        public DbSet<LinkedPropertyAttr> LinkedProperties { get; set; } = null!;

        public DbSet<AttrPropertyDetails> AttrPropertyDetails { get; set; } = null!;
        public DbSet<AttrValuationDetails> AttrValuationDetails { get; set; } = null!;
        public DbSet<AttrAccess> AttrAccess { get; set; } = null!;
        public DbSet<AttrContactInfo> AttrContactInfo { get; set; } = null!;
        public DbSet<AttrPrimaryAttributes> AttrPrimaryAttributes { get; set; } = null!;
        public DbSet<AttrSecondaryAttributes> AttrSecondaryAttributes { get; set; } = null!;
        public DbSet<AttrCalculations> AttrCalculations { get; set; } = null!;

        public DbSet<AttrRepresentative> AttrRepresentatives { get; set; } = null!;
        public DbSet<AttrDeclaration> AttrDeclarations { get; set; } = null!;

        public DbSet<AttrBusinessBuildings> AttrBusinessBuildings { get; set; } = null!;
        public DbSet<AttrBusinessSections> AttrBusinessSections { get; set; } = null!;
        public DbSet<AttrBusinessGeneral> AttrBusinessGeneral { get; set; } = null!;

        public DbSet<AttrDrcBuildings> AttrDrcBuildings { get; set; } = null!;
        public DbSet<AttrDrcImprovements> AttrDrcImprovements { get; set; } = null!;
        public DbSet<AttrDrcVacantLand> AttrDrcVacantLand { get; set; } = null!;
        public DbSet<AttrDrcMarketValueDemolition> AttrDrcMarketValueDemolition { get; set; } = null!;

        public DbSet<AttrPropertyInfo> AttrPropertyInfo { get; set; } = null!;
        public DbSet<AttrPropertyInfoAuditTrail> AttrPropertyInfoAuditTrail { get; set; } = null!;
        public DbSet<AttrWithdrawals> AttrWithdrawals { get; set; } = null!;
        public DbSet<AttrFiles> AttrFiles { get; set; } = null!;

        public DbSet<Sector> Sectors { get; set; } = null!;

        public DbSet<AttrValuerAssignment> AttrValuerAssignments { get; set; } = null!;

        public DbSet<AttrValuerReview> AttrValuerReviews { get; set; } = null!;
        public DbSet<AttrValuerReviewSection> AttrValuerReviewSections { get; set; } = null!;

        public DbSet<AttrInspectionRequest> AttrInspectionRequests { get; set; } = null!;
        public DbSet<AttrInspectionRequestSlot> AttrInspectionRequestSlots { get; set; } = null!;

        public DbSet<AttrValuerInspectionDetail> AttrValuerInspectionDetails { get; set; } = null!;
        public DbSet<AivsEmailLog> AivsEmailLogs { get; set; } = null!;
        public DbSet<AivsNotification> AivsNotifications { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Sector>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("Sectors", "dbo");

                entity.Property(e => e.TOWN_NAME_DESC)
                    .HasColumnName("TOWN_NAME_DESC");

                entity.Property(e => e.SECTOR)
                    .HasColumnName("SECTOR");
            });
            modelBuilder.Entity<AttrValuerAssignment>(entity =>
            {
                entity.ToTable("AttrValuerAssignments", "dbo");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Attr_ID).HasColumnName("Attr_ID");
                entity.Property(e => e.Attr_No).HasColumnName("Attr_No");

                entity.Property(e => e.AssignedToUserId).HasColumnName("AssignedToUserId");
                entity.Property(e => e.AssignedToUsername).HasColumnName("AssignedToUsername");
                entity.Property(e => e.AssignedToName).HasColumnName("AssignedToName");
                entity.Property(e => e.AssignedToEmail).HasColumnName("AssignedToEmail");
                entity.Property(e => e.AssignedToRole).HasColumnName("AssignedToRole");

                entity.Property(e => e.AssignedSector).HasColumnName("AssignedSector");

                entity.Property(e => e.AssignmentType).HasColumnName("AssignmentType");
                entity.Property(e => e.AssignmentStatus).HasColumnName("AssignmentStatus");

                entity.Property(e => e.AssignedByUserId).HasColumnName("AssignedByUserId");
                entity.Property(e => e.AssignedByUsername).HasColumnName("AssignedByUsername");
                entity.Property(e => e.AssignedByName).HasColumnName("AssignedByName");

                entity.Property(e => e.AssignedAt).HasColumnName("AssignedAt");

                entity.Property(e => e.ReleasedAt).HasColumnName("ReleasedAt");
                entity.Property(e => e.ReleasedByUserId).HasColumnName("ReleasedByUserId");
                entity.Property(e => e.ReleaseReason).HasColumnName("ReleaseReason");

                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
                entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
                entity.Property(e => e.UpdatedBy).HasColumnName("UpdatedBy");
                entity.Property(e => e.UpdatedDate).HasColumnName("UpdatedDate");
            });
            modelBuilder.Entity<AttrValuerReview>(entity =>
            {
                entity.ToTable("AttrValuerReviews", "dbo");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Attr_ID).HasColumnName("Attr_ID");
                entity.Property(e => e.Attr_No).HasColumnName("Attr_No");
                entity.Property(e => e.AssignmentId).HasColumnName("AssignmentId");

                entity.Property(e => e.ReviewerUserId).HasColumnName("ReviewerUserId");
                entity.Property(e => e.ReviewerUsername).HasColumnName("ReviewerUsername");
                entity.Property(e => e.ReviewerName).HasColumnName("ReviewerName");
                entity.Property(e => e.ReviewerEmail).HasColumnName("ReviewerEmail");
                entity.Property(e => e.ReviewerRole).HasColumnName("ReviewerRole");

                entity.Property(e => e.ReviewStatus).HasColumnName("ReviewStatus");
                entity.Property(e => e.StartedAt).HasColumnName("StartedAt");
                entity.Property(e => e.CompletedAt).HasColumnName("CompletedAt");

                entity.Property(e => e.FinalDecision).HasColumnName("FinalDecision");
                entity.Property(e => e.FinalComment).HasColumnName("FinalComment");

                entity.Property(e => e.RequiresInspection).HasColumnName("RequiresInspection");
                entity.Property(e => e.ReturnToClient).HasColumnName("ReturnToClient");
                entity.Property(e => e.ReadyForOvvioExtract).HasColumnName("ReadyForOvvioExtract");

                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
                entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
                entity.Property(e => e.UpdatedBy).HasColumnName("UpdatedBy");
                entity.Property(e => e.UpdatedDate).HasColumnName("UpdatedDate");
            });

            modelBuilder.Entity<AttrValuerReviewSection>(entity =>
            {
                entity.ToTable("AttrValuerReviewSections", "dbo");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.ReviewId).HasColumnName("ReviewId");
                entity.Property(e => e.Attr_ID).HasColumnName("Attr_ID");
                entity.Property(e => e.Attr_No).HasColumnName("Attr_No");

                entity.Property(e => e.SectionCode).HasColumnName("SectionCode");
                entity.Property(e => e.SectionName).HasColumnName("SectionName");

                entity.Property(e => e.SectionDecision).HasColumnName("SectionDecision");
                entity.Property(e => e.SectionComment).HasColumnName("SectionComment");

                entity.Property(e => e.RequiresCorrection).HasColumnName("RequiresCorrection");
                entity.Property(e => e.RequiresInspection).HasColumnName("RequiresInspection");

                entity.Property(e => e.ReviewedByUserId).HasColumnName("ReviewedByUserId");
                entity.Property(e => e.ReviewedByName).HasColumnName("ReviewedByName");
                entity.Property(e => e.ReviewedAt).HasColumnName("ReviewedAt");

                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
                entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
                entity.Property(e => e.UpdatedBy).HasColumnName("UpdatedBy");
                entity.Property(e => e.UpdatedDate).HasColumnName("UpdatedDate");
            });
            modelBuilder.Entity<AttrInspectionRequest>(entity =>
            {
                entity.ToTable("AttrInspectionRequests", "dbo");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Attr_ID).HasColumnName("Attr_ID");
                entity.Property(e => e.Attr_No).HasColumnName("Attr_No");
                entity.Property(e => e.ReviewId).HasColumnName("ReviewId");

                entity.Property(e => e.RequestedByUserId).HasColumnName("RequestedByUserId");
                entity.Property(e => e.RequestedByUsername).HasColumnName("RequestedByUsername");
                entity.Property(e => e.RequestedByName).HasColumnName("RequestedByName");
                entity.Property(e => e.RequestedByEmail).HasColumnName("RequestedByEmail");

                entity.Property(e => e.ClientName).HasColumnName("ClientName");
                entity.Property(e => e.ClientEmail).HasColumnName("ClientEmail");
                entity.Property(e => e.ClientCellNo).HasColumnName("ClientCellNo");

                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.ClientResponseChannel).HasColumnName("ClientResponseChannel");
                entity.Property(e => e.ClientResponseComment).HasColumnName("ClientResponseComment");
                entity.Property(e => e.ClientRespondedAt).HasColumnName("ClientRespondedAt");

                entity.Property(e => e.ConfirmedSlotId).HasColumnName("ConfirmedSlotId");
                entity.Property(e => e.ConfirmedDateTime).HasColumnName("ConfirmedDateTime");

                entity.Property(e => e.RequestComment).HasColumnName("RequestComment");
                entity.Property(e => e.EmailToken).HasColumnName("EmailToken");
                entity.Property(e => e.EmailTokenExpiresAt).HasColumnName("EmailTokenExpiresAt");

                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
                entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
                entity.Property(e => e.UpdatedBy).HasColumnName("UpdatedBy");
                entity.Property(e => e.UpdatedDate).HasColumnName("UpdatedDate");

                entity.Property(e => e.InspectionPin).HasColumnName("InspectionPin");
                entity.Property(e => e.InspectionPinGeneratedAt).HasColumnName("InspectionPinGeneratedAt");
                entity.Property(e => e.ValuerDetailsSent).HasColumnName("ValuerDetailsSent");
                entity.Property(e => e.ValuerDetailsSentAt).HasColumnName("ValuerDetailsSentAt");
                entity.Property(e => e.ValuerDetailsSentByUserId).HasColumnName("ValuerDetailsSentByUserId");
                entity.Property(e => e.ValuerDetailsSentByName).HasColumnName("ValuerDetailsSentByName");
                
                entity.Property(e => e.ValuerSapNumber)
    .HasColumnName("ValuerSapNumber")
    .HasMaxLength(50);

                entity.Property(e => e.ExpiredAt)
                    .HasColumnName("ExpiredAt");

                entity.Property(e => e.ExpiryReason)
                    .HasColumnName("ExpiryReason")
                    .HasMaxLength(500);

                entity.Property(e => e.PinVerifiedAt)
                    .HasColumnName("PinVerifiedAt");

                entity.Property(e => e.PinVerifiedByEmail)
                    .HasColumnName("PinVerifiedByEmail")
                    .HasMaxLength(255);

                entity.Property(e => e.PinFailedAttempts)
                    .HasColumnName("PinFailedAttempts");

                entity.HasMany(e => e.Slots)
                    .WithOne(e => e.InspectionRequest)
                    .HasForeignKey(e => e.InspectionRequestId);
                entity.Property(e => e.PinValidFrom)
    .HasColumnName("PinValidFrom");

                entity.Property(e => e.PinValidUntil)
                    .HasColumnName("PinValidUntil");

                entity.Property(e => e.PinUsedAt)
                    .HasColumnName("PinUsedAt");

                entity.Property(e => e.PinUsedByEmail)
                    .HasColumnName("PinUsedByEmail")
                    .HasMaxLength(255);

                entity.Property(e => e.PinUsedIpAddress)
                    .HasColumnName("PinUsedIpAddress")
                    .HasMaxLength(100);

                entity.Property(e => e.PinUsedUserAgent)
                    .HasColumnName("PinUsedUserAgent")
                    .HasMaxLength(500);
            });

            modelBuilder.Entity<AttrInspectionRequestSlot>(entity =>
            {
                entity.ToTable("AttrInspectionRequestSlots", "dbo");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.InspectionRequestId).HasColumnName("InspectionRequestId");
                entity.Property(e => e.Attr_ID).HasColumnName("Attr_ID");
                entity.Property(e => e.Attr_No).HasColumnName("Attr_No");
                entity.Property(e => e.SlotNo).HasColumnName("SlotNo");
                entity.Property(e => e.ProposedDateTime).HasColumnName("ProposedDateTime");
                entity.Property(e => e.SlotStatus).HasColumnName("SlotStatus");
                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
                entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
            });

            modelBuilder.Entity<AttrValuerInspectionDetail>(entity =>
            {
                entity.ToTable("AttrValuerInspectionDetails", "dbo");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.SapNumber).HasColumnName("SapNumber");
                entity.Property(e => e.ValuerName).HasColumnName("ValuerName");
                entity.Property(e => e.EmailAddress).HasColumnName("EmailAddress");
                entity.Property(e => e.CellNumber).HasColumnName("CellNumber");
                entity.Property(e => e.VehicleRegistration).HasColumnName("VehicleRegistration");
                entity.Property(e => e.VehicleMake).HasColumnName("VehicleMake");
                entity.Property(e => e.VehicleColour).HasColumnName("VehicleColour");
                entity.Property(e => e.PhotoFileName).HasColumnName("PhotoFileName");
                entity.Property(e => e.PhotoPath).HasColumnName("PhotoPath");
                entity.Property(e => e.IsActive).HasColumnName("IsActive");
                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
                entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
                entity.Property(e => e.UpdatedBy).HasColumnName("UpdatedBy");
                entity.Property(e => e.UpdatedDate).HasColumnName("UpdatedDate");
            });
            modelBuilder.Entity<AivsEmailLog>(entity =>
            {
                entity.ToTable("AivsEmailLogs", "dbo");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.EmailType).HasColumnName("EmailType");
                entity.Property(e => e.Attr_No).HasColumnName("Attr_No");
                entity.Property(e => e.Attr_ID).HasColumnName("Attr_ID");
                entity.Property(e => e.OriginalToEmail).HasColumnName("OriginalToEmail");
                entity.Property(e => e.ActualToEmail).HasColumnName("ActualToEmail");
                entity.Property(e => e.CcEmails).HasColumnName("CcEmails");
                entity.Property(e => e.BccEmails).HasColumnName("BccEmails");
                entity.Property(e => e.Subject).HasColumnName("Subject");
                entity.Property(e => e.BodyPreview).HasColumnName("BodyPreview");
                entity.Property(e => e.IsTestMode).HasColumnName("IsTestMode");
                entity.Property(e => e.SendStatus).HasColumnName("SendStatus");
                entity.Property(e => e.ErrorMessage).HasColumnName("ErrorMessage");
                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
                entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
                entity.Property(e => e.SentDate).HasColumnName("SentDate");
            });
            modelBuilder.Entity<AivsNotification>(entity =>
            {
                entity.ToTable("AivsNotifications", "dbo");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.TargetUserId).HasColumnName("TargetUserId");
                entity.Property(e => e.TargetUsername).HasColumnName("TargetUsername");
                entity.Property(e => e.TargetRole).HasColumnName("TargetRole");
                entity.Property(e => e.Title).HasColumnName("Title");
                entity.Property(e => e.Message).HasColumnName("Message");
                entity.Property(e => e.NotificationType).HasColumnName("NotificationType");
                entity.Property(e => e.Attr_ID).HasColumnName("Attr_ID");
                entity.Property(e => e.Attr_No).HasColumnName("Attr_No");
                entity.Property(e => e.InspectionRequestId).HasColumnName("InspectionRequestId");
                entity.Property(e => e.IsRead).HasColumnName("IsRead");
                entity.Property(e => e.ReadDateTime).HasColumnName("ReadDateTime");
                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
                entity.Property(e => e.CreatedDateTime).HasColumnName("CreatedDateTime");
            });
        }
    }
}