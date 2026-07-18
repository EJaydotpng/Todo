using System;
using System.Collections.Generic;

namespace TodoApp.Models
{
    public enum FinanceCategoryType
    {
        INCOME,
        EXPENSE,
        TRANSFER
    }

    public class FinanceCategory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ParentCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public FinanceCategoryType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public FinanceCategory? ParentCategory { get; set; }
        public ICollection<FinanceCategory> ChildCategories { get; set; } = new List<FinanceCategory>();
        public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
    }
}
