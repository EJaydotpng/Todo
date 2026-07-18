using System;
using System.Collections.Generic;

namespace TodoApp.Models
{
    public class FinanceTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public string BaseCurrency { get; set; } = "USD";
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
    }
}
