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
        }
    }
}