using System;
using System.Collections.Generic;

namespace TodoApp.Models
{
    public class FinanceTag
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        // Navigation Properties
        public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
    }
}
