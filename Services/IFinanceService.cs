using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TodoApp.Models;

namespace TodoApp.Services
{
    public interface IFinanceService
    {
        // 1. Account Management
        Task<FinanceAccount> CreateAccountAsync(string name, FinanceAccountType type, string currencyCode = "USD");
        Task<List<FinanceAccount>> GetAllAccountsAsync(bool includeInactive = false);
        Task<decimal> GetAccountBalanceAsync(Guid accountId);

        // 2. Hierarchical Category Management
        Task<FinanceCategory> CreateCategoryAsync(string name, FinanceCategoryType type, Guid? parentCategoryId = null);
        Task<List<FinanceCategory>> GetRootCategoriesAsync();
        Task<List<FinanceCategory>> GetChildCategoriesAsync(Guid parentCategoryId);
        Task<List<FinanceCategory>> GetAllCategoriesFlatAsync();

        // 3. Tag Management
        Task<FinanceTag> CreateTagAsync(string name);
        Task<List<FinanceTag>> GetAllTagsAsync();

        // 4. Transaction & Ledger Bookkeeping
        Task<FinanceTransaction> RecordTransactionAsync(
            DateTime date, 
            string description, 
            decimal totalAmount, 
            string baseCurrency, 
            List<LedgerEntryDto> entries);

        Task<List<FinanceTransaction>> GetTransactionsAsync(
            Guid? accountId = null, 
            Guid? categoryId = null, 
            string? tagName = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null);
    }

    // Data Transfer Object for recording split ledger lines
    public class LedgerEntryDto
    {
        public Guid? AccountId { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal Amount { get; set; } // Positive = Debit, Negative = Credit
        public string Memo { get; set; } = string.Empty;
        public bool IsTaxPortion { get; set; }
        public List<string> TagNames { get; set; } = new List<string>();
    }
}
