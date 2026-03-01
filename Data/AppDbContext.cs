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
        }
    }
}
