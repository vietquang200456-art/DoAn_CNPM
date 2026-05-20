using System;

namespace PharmaCheck.Models
{
    public class DrugInteraction
    {
        public int Id { get; set; }

        public int SourceDrugId { get; set; }

        public int TargetDrugId { get; set; }

        public int SeverityLevel { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Recommendation { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Drug? SourceDrug { get; set; }

        public Drug? TargetDrug { get; set; }
    }
}