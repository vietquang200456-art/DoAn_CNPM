using System;
using System.Collections.Generic;

namespace PharmaCheck.Models
{
    /// <summary>
    /// ViewModel để quản lý danh sách thuốc phân trang
    /// </summary>
    public class DrugPagedListViewModel
    {
        public IEnumerable<Drug> Drugs { get; set; } = new List<Drug>();
        
        public int CurrentPage { get; set; } = 1;
        
        public int TotalRecords { get; set; } = 0;
        
        public int PageSize { get; set; } = 10;
        
        public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;
        
        public bool HasPreviousPage => CurrentPage > 1;
        
        public bool HasNextPage => CurrentPage < TotalPages;
        
        public string SearchTerm { get; set; } = string.Empty;
        
        public string StatusFilter { get; set; } = string.Empty;
    }
}
