using System;
using System.Collections.Generic;

namespace PharmaCheck.Models
{
    public class Disease
    {
        public int Id { get; set; } // mã bệnh

        public string Name { get; set; } = string.Empty;    // tên bệnh

        public string Symptoms { get; set; } = string.Empty;    // triệu chứng

        public string Causes { get; set; } = string.Empty; // nguyên nhân

        public string TreatmentMethod { get; set; } = string.Empty; // phương pháp điều trị

        public string Description { get; set; } = string.Empty; // mô tả thêm về bệnh

        public bool IsActive { get; set; } = true; // trạng thái hoạt động của bệnh

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<DrugDiseaseContraindication> DrugDiseaseContraindications { get; set; }
            = new List<DrugDiseaseContraindication>();
    }
}