using System;
using System.Collections.Generic;

namespace TodoApp.Models
{
    public class LedgerEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TransactionId { get; set; }
        public Guid? AccountId { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string Memo { get; set; } = string.Empty;
        public bool IsTaxPortion { get; set; } = false;

        // Navigation Properties
        public FinanceTransaction Transaction { get; set; } = null!;
        public FinanceAccount? Account { get; set; }
        public FinanceCategory? Category { get; set; }
        public ICollection<FinanceTag> Tags { get; set; } = new List<FinanceTag>();
    }
}
