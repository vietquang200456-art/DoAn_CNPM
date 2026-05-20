using System;

namespace PharmaCheck.Models
{
    public class DrugDiseaseContraindication
    {
        public int Id { get; set; }

        public int DrugId { get; set; }

        public int DiseaseId { get; set; }

        public int RiskLevel { get; set; }

        public string Warning { get; set; } = string.Empty;

        public string Risk { get; set; } = string.Empty;

        public string Recommendation { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Drug? Drug { get; set; }

        public Disease? Disease { get; set; }
    }
}