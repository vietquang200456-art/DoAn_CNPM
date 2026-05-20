using Microsoft.EntityFrameworkCore;
using PharmaCheck.Models;

namespace PharmaCheck.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Drug> Drugs { get; set; }

        public DbSet<Disease> Diseases { get; set; }

        public DbSet<DrugInteraction> DrugInteractions { get; set; }

        public DbSet<DrugDiseaseContraindication> DrugDiseaseContraindications { get; set; }

        public DbSet<SearchHistory> SearchHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // DrugInteraction relationships
            modelBuilder.Entity<DrugInteraction>()
                .HasOne(di => di.SourceDrug)
                .WithMany(d => d.DrugInteractionsAsSourceDrug)
                .HasForeignKey(di => di.SourceDrugId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DrugInteraction>()
                .HasOne(di => di.TargetDrug)
                .WithMany(d => d.DrugInteractionsAsTargetDrug)
                .HasForeignKey(di => di.TargetDrugId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}