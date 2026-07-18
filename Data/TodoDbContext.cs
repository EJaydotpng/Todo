using Microsoft.EntityFrameworkCore;
using TodoApp.Models;

namespace TodoApp.Data
{
    public class TodoDbContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }

        // Finance Module DbSets
        public DbSet<FinanceAccount> FinanceAccounts { get; set; }
        public DbSet<FinanceCategory> FinanceCategories { get; set; }
        public DbSet<FinanceTag> FinanceTags { get; set; }
        public DbSet<FinanceTransaction> FinanceTransactions { get; set; }
        public DbSet<LedgerEntry> LedgerEntries { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Path: C:\Users\<Username>\AppData\Local\TodoStudio
            string appDataFolder = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), 
                "TodoStudio");

            // Ensure the directory exists
            System.IO.Directory.CreateDirectory(appDataFolder);

            string dbPath = System.IO.Path.Combine(appDataFolder, "todo.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure self-referencing relationship for Subtasks
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.ParentTask)
                .WithMany(t => t.Subtasks)
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.Cascade); // If parent task is deleted, delete subtasks

            // Configure relationship between Category and TaskItem
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Tasks)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull); // Keep task if category is deleted

            // ================= FINANCE MODULE RELATIONSHIPS =================

            // Configure self-referencing relationship for FinanceCategory (Infinite-depth categories tree)
            modelBuilder.Entity<FinanceCategory>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.ChildCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure relationships for LedgerEntry (Debit/Credit splits)
            modelBuilder.Entity<LedgerEntry>()
                .HasOne(le => le.Transaction)
                .WithMany(t => t.LedgerEntries)
                .HasForeignKey(le => le.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LedgerEntry>()
                .HasOne(le => le.Account)
                .WithMany(a => a.LedgerEntries)
                .HasForeignKey(le => le.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LedgerEntry>()
                .HasOne(le => le.Category)
                .WithMany(c => c.LedgerEntries)
                .HasForeignKey(le => le.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure many-to-many relationship for LedgerEntry and FinanceTag (Orthogonal tags)
            modelBuilder.Entity<LedgerEntry>()
                .HasMany(le => le.Tags)
                .WithMany(t => t.LedgerEntries)
                .UsingEntity(j => j.ToTable("LedgerEntryTags"));
        }
    }
}
