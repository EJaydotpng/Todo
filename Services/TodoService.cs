using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Services
{
    public class TodoService : ITodoService
    {
        private readonly TodoDbContext _dbContext;

        public TodoService()
        {
            _dbContext = new TodoDbContext();
        }

        public async Task EnsureDatabaseCreatedAsync()
        {
            await _dbContext.Database.EnsureCreatedAsync();

            // Safe SQLite migration to add SubCategory column to TaskItems if it doesn't exist
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE TaskItems ADD COLUMN SubCategory TEXT;");
            }
            catch
            {
                // Ignored (column already exists or DB was already created with it)
            }
        }

        // Category CRUD
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _dbContext.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category> AddCategoryAsync(string name)
        {
            var category = new Category { Name = name };
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();
            return category;
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            _dbContext.Entry(category).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _dbContext.Categories.FindAsync(id);
            if (category != null)
            {
                _dbContext.Categories.Remove(category);
                await _dbContext.SaveChangesAsync();
            }
        }

        // Task CRUD
        public async Task<List<TaskItem>> GetRootTasksAsync(bool isFinished)
        {
            return await _dbContext.TaskItems
                .Include(t => t.Category)
                .Include(t => t.Subtasks)
                .Where(t => t.ParentTaskId == null && t.IsFinished == isFinished)
                .OrderByDescending(t => t.DateStarted)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetRootTasksByCategoryAsync(int? categoryId, bool isFinished)
        {
            return await _dbContext.TaskItems
                .Include(t => t.Category)
                .Include(t => t.Subtasks)
                .Where(t => t.ParentTaskId == null && t.CategoryId == categoryId && t.IsFinished == isFinished)
                .OrderByDescending(t => t.DateStarted)
                .ToListAsync();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id)
        {
            return await _dbContext.TaskItems
                .Include(t => t.Category)
                .Include(t => t.Subtasks)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TaskItem> AddTaskAsync(TaskItem task)
        {
            _dbContext.TaskItems.Add(task);
            await _dbContext.SaveChangesAsync();
            return task;
        }

        public async Task UpdateTaskAsync(TaskItem task)
        {
            _dbContext.Entry(task).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteTaskAsync(int id)
        {
            var task = await _dbContext.TaskItems.FindAsync(id);
            if (task != null)
            {
                _dbContext.TaskItems.Remove(task);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task ExportDatabaseAsync(string destinationPath)
        {
            // Ensure any pending EF changes are saved
            await _dbContext.SaveChangesAsync();

            // Resolve local db path
            string appDataFolder = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), 
                "TodoStudio");
            string dbPath = System.IO.Path.Combine(appDataFolder, "todo.db");

            if (System.IO.File.Exists(dbPath))
            {
                System.IO.File.Copy(dbPath, destinationPath, overwrite: true);
                await Task.CompletedTask;
            }
            else
            {
                throw new System.IO.FileNotFoundException("Database file not found at " + dbPath);
            }
        }

        public async Task ImportDatabaseAsync(string sourcePath)
        {
            if (!System.IO.File.Exists(sourcePath))
            {
                throw new System.IO.FileNotFoundException("Source backup file not found at " + sourcePath);
            }

            // Resolve local db path
            string appDataFolder = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), 
                "TodoStudio");
            string dbPath = System.IO.Path.Combine(appDataFolder, "todo.db");

            // Close connection to unlock file
            await _dbContext.Database.CloseConnectionAsync();

            // Overwrite database file
            System.IO.File.Copy(sourcePath, dbPath, overwrite: true);

            // Discard tracked entities and establish fresh state
            _dbContext.ChangeTracker.Clear();

            // Re-open/initialize database connection
            await _dbContext.Database.OpenConnectionAsync();
        }
    }
}
