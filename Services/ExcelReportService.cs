using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Services
{
    public class ExcelReportService : IExcelReportService
    {
        static ExcelReportService()
        {
            // Set license context for EPPlus using the modern non-obsolete API
            ExcelPackage.License.SetNonCommercialPersonal("TODO Studio User");
        }

        public async Task GenerateReportAsync(string filePath)
        {
            List<Category> categories;
            List<TaskItem> uncategorizedTasks;

            using (var db = new TodoDbContext())
            {
                // Fetch all categories with their tasks and subtasks
                categories = await db.Categories
                    .Include(c => c.Tasks)
                        .ThenInclude(t => t.Subtasks)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                // Fetch all root level uncategorized tasks
                uncategorizedTasks = await db.TaskItems
                    .Include(t => t.Subtasks)
                    .Where(t => t.CategoryId == null && t.ParentTaskId == null)
                    .OrderByDescending(t => t.DateStarted)
                    .ToListAsync();
            }

            // Create flat list of rows for the Excel table
            var rows = new List<ExcelTaskRow>();

            // Process Categorized tasks
            foreach (var category in categories)
            {
                var rootTasks = category.Tasks.Where(t => t.ParentTaskId == null).ToList();
                foreach (var task in rootTasks)
                {
                    rows.Add(new ExcelTaskRow
                    {
                        Category = category.Name,
                        SubCategory = task.SubCategory ?? "",
                        Title = task.Title,
                        IsSubtask = "No",
                        ParentTask = "",
                        Description = task.Description ?? "",
                        DateStarted = task.DateStarted,
                        DateFinished = task.DateFinished,
                        Status = task.IsFinished ? "Finished" : "Pending"
                    });

                    if (task.Subtasks != null)
                    {
                        foreach (var subtask in task.Subtasks)
                        {
                            rows.Add(new ExcelTaskRow
                            {
                                Category = category.Name,
                                SubCategory = subtask.SubCategory ?? task.SubCategory ?? "",
                                Title = subtask.Title,
                                IsSubtask = "Yes",
                                ParentTask = task.Title,
                                Description = subtask.Description ?? "",
                                DateStarted = subtask.DateStarted,
                                DateFinished = subtask.DateFinished,
                                Status = subtask.IsFinished ? "Finished" : "Pending"
                            });
                        }
                    }
                }
            }

            // Process Uncategorized tasks
            foreach (var task in uncategorizedTasks)
            {
                rows.Add(new ExcelTaskRow
                {
                    Category = "Uncategorized",
                    SubCategory = task.SubCategory ?? "",
                    Title = task.Title,
                    IsSubtask = "No",
                    ParentTask = "",
                    Description = task.Description ?? "",
                    DateStarted = task.DateStarted,
                    DateFinished = task.DateFinished,
                    Status = task.IsFinished ? "Finished" : "Pending"
                });

                if (task.Subtasks != null)
                {
                    foreach (var subtask in task.Subtasks)
                    {
                        rows.Add(new ExcelTaskRow
                        {
                            Category = "Uncategorized",
                            SubCategory = subtask.SubCategory ?? task.SubCategory ?? "",
                            Title = subtask.Title,
                            IsSubtask = "Yes",
                            ParentTask = task.Title,
                            Description = subtask.Description ?? "",
                            DateStarted = subtask.DateStarted,
                            DateFinished = subtask.DateFinished,
                            Status = subtask.IsFinished ? "Finished" : "Pending"
                        });
                    }
                }
            }

            // Generate the Excel package
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Tasks Report");
                worksheet.View.ShowGridLines = true;

                // Title Block
                worksheet.Cells["A1"].Value = "TODO Application - Tasks Report";
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(79, 70, 229)); // Indigo

                worksheet.Cells["A2"].Value = $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                worksheet.Cells["A2"].Style.Font.Size = 10;
                worksheet.Cells["A2"].Style.Font.Italic = true;
                worksheet.Cells["A2"].Style.Font.Color.SetColor(System.Drawing.Color.Gray);

                // Summary Statistics Cards
                int totalCount = rows.Count;
                int pendingCount = rows.Count(r => r.Status == "Pending");
                int finishedCount = rows.Count(r => r.Status == "Finished");

                worksheet.Cells["A4"].Value = "Summary Stats";
                worksheet.Cells["A4"].Style.Font.Bold = true;
                worksheet.Cells["A4"].Style.Font.Size = 11;

                worksheet.Cells["A5"].Value = "Total Tasks";
                worksheet.Cells["B5"].Value = totalCount;
                worksheet.Cells["A6"].Value = "Pending Tasks";
                worksheet.Cells["B6"].Value = pendingCount;
                worksheet.Cells["A7"].Value = "Finished Tasks";
                worksheet.Cells["B7"].Value = finishedCount;

                // Format summary box
                using (var range = worksheet.Cells["A4:B7"])
                {
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(243, 244, 246)); // light gray background
                }
                worksheet.Cells["A4:A7"].Style.Font.Bold = true;
                worksheet.Cells["B5:B7"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                // Table Headers
                string[] headers = {
                    "Category", "Sub-Category", "Task Title", "Is Subtask",
                    "Parent Task Title", "Description", "Date Started", "Date Finished", "Status"
                };

                int startRow = 9;
                for (int col = 0; col < headers.Length; col++)
                {
                    var cell = worksheet.Cells[startRow, col + 1];
                    cell.Value = headers[col];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(79, 70, 229)); // Indigo
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                }

                // Data Rows
                int currentRow = startRow + 1;
                foreach (var row in rows)
                {
                    worksheet.Cells[currentRow, 1].Value = row.Category;
                    worksheet.Cells[currentRow, 2].Value = row.SubCategory;
                    worksheet.Cells[currentRow, 3].Value = row.Title;
                    worksheet.Cells[currentRow, 4].Value = row.IsSubtask;
                    worksheet.Cells[currentRow, 5].Value = row.ParentTask;
                    worksheet.Cells[currentRow, 6].Value = row.Description;

                    // Date Started
                    var cellDateStarted = worksheet.Cells[currentRow, 7];
                    cellDateStarted.Value = row.DateStarted;
                    cellDateStarted.Style.Numberformat.Format = "yyyy-mm-dd hh:mm";

                    // Date Finished
                    var cellDateFinished = worksheet.Cells[currentRow, 8];
                    if (row.DateFinished.HasValue)
                    {
                        cellDateFinished.Value = row.DateFinished.Value;
                        cellDateFinished.Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
                    }
                    else
                    {
                        cellDateFinished.Value = "";
                    }

                    // Status
                    var cellStatus = worksheet.Cells[currentRow, 9];
                    cellStatus.Value = row.Status;
                    cellStatus.Style.Font.Bold = true;
                    if (row.Status == "Finished")
                    {
                        cellStatus.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(22, 101, 52)); // dark green
                    }
                    else
                    {
                        cellStatus.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(194, 65, 12)); // dark orange
                    }

                    currentRow++;
                }

                // Enable AutoFilter on the entire data range
                var dataRange = worksheet.Cells[startRow, 1, currentRow - 1, headers.Length];
                dataRange.AutoFilter = true;

                // Add thin borders to all data cells
                using (var cells = worksheet.Cells[startRow, 1, currentRow - 1, headers.Length])
                {
                    cells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    cells.Style.Border.Top.Color.SetColor(System.Drawing.Color.LightGray);
                    cells.Style.Border.Bottom.Color.SetColor(System.Drawing.Color.LightGray);
                    cells.Style.Border.Left.Color.SetColor(System.Drawing.Color.LightGray);
                    cells.Style.Border.Right.Color.SetColor(System.Drawing.Color.LightGray);
                }

                // Auto-fit column widths
                worksheet.Cells[startRow, 1, currentRow - 1, headers.Length].AutoFitColumns();

                // Save to file
                var fileInfo = new FileInfo(filePath);
                await package.SaveAsAsync(fileInfo);
            }
        }

        private class ExcelTaskRow
        {
            public string Category { get; set; } = string.Empty;
            public string SubCategory { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string IsSubtask { get; set; } = string.Empty;
            public string ParentTask { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime DateStarted { get; set; }
            public DateTime? DateFinished { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}
