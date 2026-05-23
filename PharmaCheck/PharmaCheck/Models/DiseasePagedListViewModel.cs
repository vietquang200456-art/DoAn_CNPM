using System;
using System.Collections.Generic;

namespace PharmaCheck.Models
{
    /// <summary>
    /// ViewModel để quản lý danh sách bệnh lý phân trang
    /// </summary>
    public class DiseasePagedListViewModel
    {
        public IEnumerable<Disease> Diseases { get; set; } = new List<Disease>();
        
        public int CurrentPage { get; set; } = 1;
        
        public int TotalRecords { get; set; } = 0;
        
        public int PageSize { get; set; } = 10;
        
        public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;
        
        public bool HasPreviousPage => CurrentPage > 1;
        
        public bool HasNextPage => CurrentPage < TotalPages;
        
        public string SearchTerm { get; set; } = string.Empty;
        
        public string SeverityFilter { get; set; } = string.Empty;
    }
}
