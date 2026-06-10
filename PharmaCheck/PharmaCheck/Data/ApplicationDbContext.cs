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
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =============================================================
            // 1. Cấu hình các mối quan hệ cho DrugInteraction
            // =============================================================
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

            // =============================================================
            // 2. BỔ SUNG: Cấu hình các mối quan hệ đa khóa ngoại cho SearchHistory
            // =============================================================
            modelBuilder.Entity<SearchHistory>(entity =>
            {
                // Mối quan hệ với Thuốc thứ nhất (DrugId) -> Trỏ về tập hợp SearchHistories trong Drug
                entity.HasOne(sh => sh.Drug)
                      .WithMany(d => d.SearchHistories) 
                      .HasForeignKey(sh => sh.DrugId)
                      .OnDelete(DeleteBehavior.Restrict); 

                // Mối quan hệ với Thuốc thứ hai (TargetDrugId) nếu có tra cứu cặp tương tác
                entity.HasOne(sh => sh.TargetDrug)
                      .WithMany() // Để trống vì không cần tạo tập hợp ngược trong Drug
                      .HasForeignKey(sh => sh.TargetDrugId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Mối quan hệ với Bệnh lý (DiseaseId) nếu có tra cứu chống chỉ định
                entity.HasOne(sh => sh.Disease)
                      .WithMany() // Để trống vì không cần tạo tập hợp ngược trong Disease
                      .HasForeignKey(sh => sh.DiseaseId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                // Mối quan hệ với User (UserId) thực hiện tìm kiếm
                entity.HasOne(sh => sh.User)
                      .WithMany() // Thay đổi thành .WithMany(u => u.SearchHistories) nếu trong class User của bạn có lưu tập hợp này
                      .HasForeignKey(sh => sh.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // Nếu xóa User thì xóa sạch lịch sử tìm kiếm của họ
            });
        }
    }
}