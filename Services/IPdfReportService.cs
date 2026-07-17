using System.Threading.Tasks;

namespace TodoApp.Services
{
    public interface IPdfReportService
    {
        Task GenerateReportAsync(string filePath);
    }
}
