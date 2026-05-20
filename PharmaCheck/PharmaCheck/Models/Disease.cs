using System;
using System.Collections.Generic;

namespace PharmaCheck.Models
{
    public class Disease
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Symptoms { get; set; } = string.Empty;

        public string Causes { get; set; } = string.Empty;

        public string TreatmentMethod { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<DrugDiseaseContraindication> DrugDiseaseContraindications { get; set; }
            = new List<DrugDiseaseContraindication>();
    }
}