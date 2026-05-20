using System;
using System.Collections.Generic;

namespace PharmaCheck.Models
{
    public class Drug
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ActiveIngredient { get; set; } = string.Empty;

        public string Function { get; set; } = string.Empty;

        public string Dosage { get; set; } = string.Empty;

        public string SideEffects { get; set; } = string.Empty;

        public string Contraindications { get; set; } = string.Empty;

        public string Manufacturer { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

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