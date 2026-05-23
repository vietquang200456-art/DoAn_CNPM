using System;
using System.Collections.Generic;

namespace PharmaCheck.Models
{
    public class Drug
    {
        public int Id { get; set; }// mã thuốc

        public string Name { get; set; } = string.Empty; // tên thuốc

        public string ActiveIngredient { get; set; } = string.Empty; // thành phần hoạt chất

        public string Function { get; set; } = string.Empty; // công dụng

        public string Dosage {get; set; } = string.Empty;  // liều lượng

        public string SideEffects { get; set; } = string.Empty; // tác dụng phụ

        public string Contraindications { get; set; } = string.Empty; // chống chỉ định

        public string Manufacturer { get; set; } = string.Empty; // nhà sản xuất

        public string Description { get; set; } = string.Empty; // mô tả thêm về thuốc

        public bool IsActive { get; set; } = true; // trạng thái hoạt động của thuốc

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int ViewCount { get; set; } = 0;

        // Navigation
        public ICollection<DrugInteraction> DrugInteractionsAsSourceDrug { get; set; }
            = new List<DrugInteraction>();

        public ICollection<DrugInteraction> DrugInteractionsAsTargetDrug { get; set; }
            = new List<DrugInteraction>();

        public ICollection<DrugDiseaseContraindication> DrugDiseaseContraindications { get; set; }
            = new List<DrugDiseaseContraindication>();

        public ICollection<SearchHistory> SearchHistories { get; set; }
            = new List<SearchHistory>();
    }
}