using AIVS.Models.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace AIVS.Data
{
    public class UserManagementDbContext : DbContext
    {
        public UserManagementDbContext(DbContextOptions<UserManagementDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserManagementUser> Users { get; set; } = null!;
        public DbSet<UserManagementUserSystem> UserSystems { get; set; } = null!;
        public DbSet<UserManagementRole> Roles { get; set; } = null!;
        public DbSet<UserManagementValuerDetail> ValuerDetails { get; set; } = null!;

       
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserManagementUser>(entity =>
            {
                entity.ToTable("Users", "dbo");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Username).HasColumnName("Username");
                entity.Property(e => e.FullName).HasColumnName("FullName");
                entity.Property(e => e.Email).HasColumnName("Email");
                entity.Property(e => e.IsActive).HasColumnName("IsActive");
            });

            modelBuilder.Entity<UserManagementUserSystem>(entity =>
            {
                entity.ToTable("UserSystems", "dbo");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.UserId).HasColumnName("UserId");
                entity.Property(e => e.SystemId).HasColumnName("SystemId");
                entity.Property(e => e.RoleId).HasColumnName("RoleId");
                entity.Property(e => e.Sector).HasColumnName("Sector");
                entity.Property(e => e.IsActive).HasColumnName("IsActive");
            });

            modelBuilder.Entity<UserManagementRole>(entity =>
            {
                entity.ToTable("Roles", "dbo");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.RoleName).HasColumnName("RoleName");
                entity.Property(e => e.IsActive).HasColumnName("IsActive");
            });

            modelBuilder.Entity<UserManagementValuerDetail>(entity =>
            {
                entity.ToTable("ValuerDetails", "dbo");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.UserId).HasColumnName("UserId");
                entity.Property(e => e.Pin).HasColumnName("Pin");
                entity.Property(e => e.VehicleRegistration).HasColumnName("VehicleRegistration");
                entity.Property(e => e.VehicleMake).HasColumnName("VehicleMake");
                entity.Property(e => e.VehicleColour).HasColumnName("VehicleColour");
                entity.Property(e => e.CellNumber).HasColumnName("CellNumber");
                entity.Property(e => e.IsActive).HasColumnName("IsActive");
            });
            
        }
    }
}