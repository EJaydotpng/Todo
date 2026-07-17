using System.Collections.Generic;
using System.Threading.Tasks;
using TodoApp.Models;

namespace TodoApp.Services
{
    public interface ITodoService
    {
        Task EnsureDatabaseCreatedAsync();
        
        // Category CRUD
        Task<List<Category>> GetAllCategoriesAsync();
        Task<Category> AddCategoryAsync(string name);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(int id);

        // Task CRUD
        Task<List<TaskItem>> GetRootTasksAsync(bool isFinished);
        Task<List<TaskItem>> GetRootTasksByCategoryAsync(int? categoryId, bool isFinished);
        Task<TaskItem?> GetTaskByIdAsync(int id);
        Task<TaskItem> AddTaskAsync(TaskItem task);
        Task UpdateTaskAsync(TaskItem task);
        Task DeleteTaskAsync(int id);

        // Database Backup & Restore
        Task ExportDatabaseAsync(string destinationPath);
        Task ImportDatabaseAsync(string sourcePath);
    }
}
