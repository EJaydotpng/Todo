using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Services
{
    public class FinanceService : IFinanceService
    {
        // 1. Account Management
        public async Task<FinanceAccount> CreateAccountAsync(string name, FinanceAccountType type, string currencyCode = "USD")
        {
            using var db = new TodoDbContext();
            var account = new FinanceAccount
            {
                Name = name,
                Type = type,
                CurrencyCode = currencyCode
            };
            db.FinanceAccounts.Add(account);
            await db.SaveChangesAsync();
            return account;
        }

        public async Task<List<FinanceAccount>> GetAllAccountsAsync(bool includeInactive = false)
        {
            using var db = new TodoDbContext();
            var query = db.FinanceAccounts.AsQueryable();
            if (!includeInactive)
            {
                query = query.Where(a => a.IsActive);
            }
            return await query.OrderBy(a => a.Name).ToListAsync();
        }

        public async Task<decimal> GetAccountBalanceAsync(Guid accountId)
        {
            using var db = new TodoDbContext();
            // Calculate sum of all ledger entry amounts for this account
            return await db.LedgerEntries
                .Where(le => le.AccountId == accountId)
                .SumAsync(le => le.Amount);
        }

        // 2. Hierarchical Category Management
        public async Task<FinanceCategory> CreateCategoryAsync(string name, FinanceCategoryType type, Guid? parentCategoryId = null)
        {
            using var db = new TodoDbContext();
            
            if (parentCategoryId.HasValue)
            {
                var parentExists = await db.FinanceCategories.AnyAsync(c => c.Id == parentCategoryId.Value);
                if (!parentExists)
                {
                    throw new ArgumentException("Parent category does not exist.");
                }
            }

            var category = new FinanceCategory
            {
                Name = name,
                Type = type,
                ParentCategoryId = parentCategoryId
            };
            db.FinanceCategories.Add(category);
            await db.SaveChangesAsync();
            return category;
        }

        public async Task<List<FinanceCategory>> GetRootCategoriesAsync()
        {
            using var db = new TodoDbContext();
            return await db.FinanceCategories
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<FinanceCategory>> GetChildCategoriesAsync(Guid parentCategoryId)
        {
            using var db = new TodoDbContext();
            return await db.FinanceCategories
                .Where(c => c.ParentCategoryId == parentCategoryId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<FinanceCategory>> GetAllCategoriesFlatAsync()
        {
            using var db = new TodoDbContext();
            return await db.FinanceCategories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        // 3. Tag Management
        public async Task<FinanceTag> CreateTagAsync(string name)
        {
            using var db = new TodoDbContext();
            string cleanName = name.Trim().ToLower();
            if (!cleanName.StartsWith("#")) cleanName = "#" + cleanName;

            var existingTag = await db.FinanceTags.FirstOrDefaultAsync(t => t.Name == cleanName);
            if (existingTag != null) return existingTag;

            var tag = new FinanceTag { Name = cleanName };
            db.FinanceTags.Add(tag);
            await db.SaveChangesAsync();
            return tag;
        }

        public async Task<List<FinanceTag>> GetAllTagsAsync()
        {
            using var db = new TodoDbContext();
            return await db.FinanceTags.OrderBy(t => t.Name).ToListAsync();
        }

        // 4. Transaction & Ledger Bookkeeping (Double-Entry Splits)
        public async Task<FinanceTransaction> RecordTransactionAsync(
            DateTime date, 
            string description, 
            decimal totalAmount, 
            string baseCurrency, 
            List<LedgerEntryDto> entries)
        {
            if (entries == null || entries.Count < 2)
            {
                throw new ArgumentException("A double-entry transaction must have at least 2 ledger lines.");
            }

            // 1. Sum Validation
            decimal totalDebits = entries.Where(e => e.Amount > 0).Sum(e => e.Amount);
            decimal totalCredits = entries.Where(e => e.Amount < 0).Sum(e => e.Amount);

            // Double entry verification: totalDebits must equal totalAmount, and totalCredits must equal -totalAmount
            if (totalDebits != totalAmount)
            {
                throw new InvalidOperationException($"Sum of debits ({totalDebits}) does not match the parent total amount ({totalAmount}).");
            }
            if (Math.Abs(totalCredits) != totalAmount)
            {
                throw new InvalidOperationException($"Sum of credits ({Math.Abs(totalCredits)}) does not match the parent total amount ({totalAmount}).");
            }

            // Zero-Sum ledger verification
            decimal zeroSumCheck = entries.Sum(e => e.Amount);
            if (zeroSumCheck != 0.0m)
            {
                throw new InvalidOperationException($"Double-entry mismatch! The sum of all splits is {zeroSumCheck}, but it must be exactly 0.00.");
            }

            using var db = new TodoDbContext();
            using var dbTransaction = await db.Database.BeginTransactionAsync();

            try
            {
                var transaction = new FinanceTransaction
                {
                    TransactionDate = date,
                    Description = description,
                    TotalAmount = totalAmount,
                    BaseCurrency = baseCurrency
                };
                db.FinanceTransactions.Add(transaction);
                await db.SaveChangesAsync(); // Generates transaction.Id

                foreach (var entryDto in entries)
                {
                    // Enforce structural rule: belong to either an Account or a Category, not neither, not both
                    if (entryDto.AccountId.HasValue && entryDto.CategoryId.HasValue)
                    {
                        throw new ArgumentException("A ledger line cannot belong to both an Account and a Category.");
                    }
                    if (!entryDto.AccountId.HasValue && !entryDto.CategoryId.HasValue)
                    {
                        throw new ArgumentException("A ledger line must belong to either an Account or a Category.");
                    }

                    var ledgerEntry = new LedgerEntry
                    {
                        TransactionId = transaction.Id,
                        AccountId = entryDto.AccountId,
                        CategoryId = entryDto.CategoryId,
                        Amount = entryDto.Amount,
                        Memo = entryDto.Memo,
                        IsTaxPortion = entryDto.IsTaxPortion
                    };

                    // Handle tags
                    foreach (var tagName in entryDto.TagNames)
                    {
                        string cleanName = tagName.Trim().ToLower();
                        if (!cleanName.StartsWith("#")) cleanName = "#" + cleanName;

                        var tag = await db.FinanceTags.FirstOrDefaultAsync(t => t.Name == cleanName);
                        if (tag == null)
                        {
                            tag = new FinanceTag { Name = cleanName };
                            db.FinanceTags.Add(tag);
                            await db.SaveChangesAsync();
                        }
                        ledgerEntry.Tags.Add(tag);
                    }

                    db.LedgerEntries.Add(ledgerEntry);
                }

                await db.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return transaction;
            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<FinanceTransaction>> GetTransactionsAsync(
            Guid? accountId = null, 
            Guid? categoryId = null, 
            string? tagName = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null)
        {
            using var db = new TodoDbContext();
            
            var query = db.FinanceTransactions
                .Include(t => t.LedgerEntries)
                    .ThenInclude(le => le.Account)
                .Include(t => t.LedgerEntries)
                    .ThenInclude(le => le.Category)
                .Include(t => t.LedgerEntries)
                    .ThenInclude(le => le.Tags)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate <= endDate.Value);
            }

            var transactions = await query.OrderByDescending(t => t.TransactionDate).ToListAsync();

            // Perform post-load filtering for complex relational conditions
            if (accountId.HasValue)
            {
                transactions = transactions.Where(t => t.LedgerEntries.Any(le => le.AccountId == accountId.Value)).ToList();
            }
            if (categoryId.HasValue)
            {
                transactions = transactions.Where(t => t.LedgerEntries.Any(le => le.CategoryId == categoryId.Value)).ToList();
            }
            if (!string.IsNullOrEmpty(tagName))
            {
                string cleanTagName = tagName.Trim().ToLower();
                if (!cleanTagName.StartsWith("#")) cleanTagName = "#" + cleanTagName;
                transactions = transactions.Where(t => t.LedgerEntries.Any(le => le.Tags.Any(tg => tg.Name == cleanTagName))).ToList();
            }

            return transactions;
        }
    }
}
