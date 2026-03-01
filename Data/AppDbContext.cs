using Microsoft.EntityFrameworkCore;
using Rota2.Models;

namespace Rota2.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Rota> Rotas { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<RotaDoctor> RotaDoctors { get; set; }
        public DbSet<RotaAdmin> RotaAdmins { get; set; }
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<RotaDoctor>().HasKey(rd => new { rd.RotaId, rd.UserId });
            modelBuilder.Entity<RotaDoctor>()
                .HasOne(rd => rd.Rota).WithMany(r => r.RotaDoctors).HasForeignKey(rd => rd.RotaId);
            modelBuilder.Entity<RotaDoctor>()
                .HasOne(rd => rd.User).WithMany().HasForeignKey(rd => rd.UserId);

            modelBuilder.Entity<RotaAdmin>().HasKey(ra => new { ra.RotaId, ra.UserId });
            modelBuilder.Entity<RotaAdmin>()
                .HasOne(ra => ra.Rota).WithMany(r => r.RotaAdmins).HasForeignKey(ra => ra.RotaId);
            modelBuilder.Entity<RotaAdmin>()
                .HasOne(ra => ra.User).WithMany().HasForeignKey(ra => ra.UserId);

            modelBuilder.Entity<ShiftAssignment>().HasKey(sa => sa.Id);
            modelBuilder.Entity<ShiftAssignment>()
                .HasOne(sa => sa.Rota).WithMany().HasForeignKey(sa => sa.RotaId);
            modelBuilder.Entity<ShiftAssignment>()
                .HasOne(sa => sa.Shift).WithMany().HasForeignKey(sa => sa.ShiftId);
            modelBuilder.Entity<ShiftAssignment>()
                .HasOne(sa => sa.User).WithMany().HasForeignKey(sa => sa.UserId);
            modelBuilder.Entity<ShiftAssignment>()
                .HasIndex(sa => new { sa.RotaId, sa.ShiftId, sa.Date }).IsUnique();
        }
    }
}
