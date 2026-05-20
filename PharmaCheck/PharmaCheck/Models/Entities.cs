using System;
using System.Collections.Generic;

namespace PharmaCheckApp.Models
{
    /// <summary>
    /// User model for authentication and authorization
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // Admin, User
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation
        public ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
    }

    /// <summary>
    /// Drug model representing pharmaceutical products
    /// </summary>
    public class Drug
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ActiveIngredient { get; set; } = string.Empty; // Hoạt chất
        public string Function { get; set; } = string.Empty; // Công dụng
        public string Dosage { get; set; } = string.Empty; // Liều dùng
        public string SideEffects { get; set; } = string.Empty; // Tác dụng phụ
        public string Contraindications { get; set; } = string.Empty; // Chống chỉ định
        public string Manufacturer { get; set; } = string.Empty; // Nhà sản xuất
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int ViewCount { get; set; } = 0;

        // Navigation
        public ICollection<DrugInteraction> DrugInteractionsAsSourceDrug { get; set; } = new List<DrugInteraction>();
        public ICollection<DrugInteraction> DrugInteractionsAsTargetDrug { get; set; } = new List<DrugInteraction>();
        public ICollection<DrugDiseaseContraindication> DrugDiseaseContraindications { get; set; } = new List<DrugDiseaseContraindication>();
        public ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
    }

    /// <summary>
    /// Disease model representing medical conditions
    /// </summary>
    public class Disease
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Symptoms { get; set; } = string.Empty; // Triệu chứng
        public string Causes { get; set; } = string.Empty; // Nguyên nhân
        public string TreatmentMethod { get; set; } = string.Empty; // Phương pháp điều trị
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<DrugDiseaseContraindication> DrugDiseaseContraindications { get; set; } = new List<DrugDiseaseContraindication>();
    }

    /// <summary>
    /// Drug-Drug Interaction model
    /// </summary>
    public class DrugInteraction
    {
        public int Id { get; set; }
        public int SourceDrugId { get; set; }
        public int TargetDrugId { get; set; }
        public int SeverityLevel { get; set; } // 1=Nhẹ, 2=Trung bình, 3=Nguy hiểm
        public string Description { get; set; } = string.Empty; // Mô tả tương tác
        public string Recommendation { get; set; } = string.Empty; // Khuyến nghị
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Drug? SourceDrug { get; set; }
        public Drug? TargetDrug { get; set; }
    }

    /// <summary>
    /// Drug-Disease Contraindication model
    /// </summary>
    public class DrugDiseaseContraindication
    {
        public int Id { get; set; }
        public int DrugId { get; set; }
        public int DiseaseId { get; set; }
        public int RiskLevel { get; set; } // 1=Thấp, 2=Trung bình, 3=Cao
        public string Warning { get; set; } = string.Empty; // Cảnh báo
        public string Risk { get; set; } = string.Empty; // Nguy cơ
        public string Recommendation { get; set; } = string.Empty; // Khuyến nghị
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Drug? Drug { get; set; }
        public Disease? Disease { get; set; }
    }

    /// <summary>
    /// Search History model for tracking user searches
    /// </summary>
    public class SearchHistory
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? DrugId { get; set; }
        public string SearchType { get; set; } = string.Empty; // DrugSearch, DiseaseSearch, InteractionCheck, ContraindicationCheck
        public string SearchQuery { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? User { get; set; }
        public Drug? Drug { get; set; }
    }
}
