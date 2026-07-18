using System;
using System.Collections.Generic;

namespace TodoApp.Models
{
    public enum FinanceAccountType
    {
        CASH,
        CREDIT,
        INVESTMENT,
        LOAN,
        RECEIVABLE
    }

    public class FinanceAccount
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public FinanceAccountType Type { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
    }
}
