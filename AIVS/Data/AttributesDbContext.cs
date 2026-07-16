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
        }
    }
}