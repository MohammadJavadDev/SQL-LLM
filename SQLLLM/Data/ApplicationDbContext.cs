using Microsoft.EntityFrameworkCore;
using SQLLLM.Models;

namespace SQLLLM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Sales> Sales { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure Sales entity
            modelBuilder.Entity<Sales>()
                .Property(s => s.Region)
                .HasMaxLength(100);
            
            modelBuilder.Entity<Sales>()
                .Property(s => s.SalesAmount)
                .HasPrecision(18, 2);
        }
    }
}