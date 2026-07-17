using System.Threading.Tasks;

namespace TodoApp.Services
{
    public interface IExcelReportService
    {
        Task GenerateReportAsync(string filePath);
    }
}
