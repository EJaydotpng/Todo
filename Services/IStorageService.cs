using System.Threading.Tasks;

namespace TodoApp.Services
{
    public interface IStorageService
    {
        Task<string?> SaveFileDialogAsync(string defaultFileName, string extension, string filterName);
        Task<string?> OpenFileDialogAsync(string extension, string filterName);
    }
}
