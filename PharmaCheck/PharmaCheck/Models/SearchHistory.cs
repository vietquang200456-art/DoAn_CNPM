using System;

namespace PharmaCheck.Models
{
    public class SearchHistory
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public int? DrugId { get; set; }

        public string SearchType { get; set; } = string.Empty;

        public string SearchQuery { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? User { get; set; }

        public Drug? Drug { get; set; }
    }
}