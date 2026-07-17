using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Services
{
    public class PdfReportService : IPdfReportService
    {
        static PdfReportService()
        {
            // Set license to community for open-source/free use
            QuestPDF.Settings.License = LicenseType.Community;
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

            // Calculations
            int totalTasksCount = categories.Sum(c => c.Tasks.Count) + uncategorizedTasks.Count;
            // Note: Since uncategorized tasks includes subtasks internally, let's count only top level + non-root.
            // But a simpler approach is to count total TaskItems in database
            int databaseTotalTasks = 0;
            int databaseFinishedTasks = 0;
            int databaseUnfinishedTasks = 0;

            using (var db = new TodoDbContext())
            {
                databaseTotalTasks = await db.TaskItems.CountAsync();
                databaseFinishedTasks = await db.TaskItems.CountAsync(t => t.IsFinished);
                databaseUnfinishedTasks = databaseTotalTasks - databaseFinishedTasks;
            }

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10));

                    // Header
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("TODO Application Report")
                                .SemiBold().FontSize(24).FontColor(Colors.Indigo.Darken3);
                            
                            column.Item().Text($"Generated on: {DateTime.Now:MMMM dd, yyyy - hh:mm tt}")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    // Content
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Spacing(20);

                        // Summary Statistics Cards
                        col.Item().Row(row =>
                        {
                            row.Spacing(15);

                            // Total Card
                            row.RelativeItem().Background(Colors.Indigo.Lighten5).Padding(10).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Total Tasks").FontSize(10).FontColor(Colors.Indigo.Darken2);
                                c.Item().AlignCenter().Text($"{databaseTotalTasks}").Bold().FontSize(20).FontColor(Colors.Indigo.Darken3);
                            });

                            // Pending Card
                            row.RelativeItem().Background(Colors.Orange.Lighten5).Padding(10).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Pending Tasks").FontSize(10).FontColor(Colors.Orange.Darken2);
                                c.Item().AlignCenter().Text($"{databaseUnfinishedTasks}").Bold().FontSize(20).FontColor(Colors.Orange.Darken3);
                            });

                            // Finished Card
                            row.RelativeItem().Background(Colors.Green.Lighten5).Padding(10).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Finished Tasks").FontSize(10).FontColor(Colors.Green.Darken2);
                                c.Item().AlignCenter().Text($"{databaseFinishedTasks}").Bold().FontSize(20).FontColor(Colors.Green.Darken3);
                            });
                        });

                        // Categories Sections
                        foreach (var category in categories)
                        {
                            // Filter tasks for this category that are root tasks
                            var categoryRootTasks = category.Tasks.Where(t => t.ParentTaskId == null).ToList();
                            if (categoryRootTasks.Count == 0) continue;

                            col.Item().Column(catCol =>
                            {
                                catCol.Spacing(5);
                                catCol.Item().Text(category.Name).Bold().FontSize(14).FontColor(Colors.Indigo.Darken2);
                                catCol.Item().BorderBottom(1).BorderColor(Colors.Indigo.Lighten4);
                                catCol.Item().PaddingTop(5).Table(table => RenderTaskTable(table, categoryRootTasks));
                            });
                        }

                        // Uncategorized Section
                        if (uncategorizedTasks.Count > 0)
                        {
                            col.Item().Column(catCol =>
                            {
                                catCol.Spacing(5);
                                catCol.Item().Text("Uncategorized").Bold().FontSize(14).FontColor(Colors.Grey.Darken3);
                                catCol.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                catCol.Item().PaddingTop(5).Table(table => RenderTaskTable(table, uncategorizedTasks));
                            });
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);
        }

        private void RenderTaskTable(TableDescriptor table, List<TaskItem> tasks)
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4); // Title & Subtasks
                columns.RelativeColumn(2); // Date Started
                columns.RelativeColumn(2); // Date Finished
                columns.RelativeColumn(1.5f); // Status
            });

            // Table Header
            table.Header(header =>
            {
                header.Cell().Background(Colors.Indigo.Darken3).Padding(6).Text("Task Title").Bold().FontColor(Colors.White).FontSize(9);
                header.Cell().Background(Colors.Indigo.Darken3).Padding(6).Text("Date Started").Bold().FontColor(Colors.White).FontSize(9);
                header.Cell().Background(Colors.Indigo.Darken3).Padding(6).Text("Date Finished").Bold().FontColor(Colors.White).FontSize(9);
                header.Cell().Background(Colors.Indigo.Darken3).Padding(6).Text("Status").Bold().FontColor(Colors.White).FontSize(9).AlignCenter();
            });

            // Table Rows
            foreach (var task in tasks)
            {
                // Title and Subtasks in Column 1
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(tc =>
                {
                    tc.Item().Row(tr =>
                    {
                        tr.RelativeItem().Text(task.Title).SemiBold();
                        if (!string.IsNullOrEmpty(task.SubCategory))
                        {
                            tr.ConstantItem(100).AlignRight().Text($"[{task.SubCategory}]").FontSize(7.5f).Bold().FontColor(Colors.Orange.Darken3);
                        }
                    });

                    if (!string.IsNullOrEmpty(task.Description))
                    {
                        tc.Item().Text(task.Description).FontSize(8).FontColor(Colors.Grey.Darken2);
                    }
                    if (task.Subtasks != null && task.Subtasks.Count > 0)
                    {
                        foreach (var sub in task.Subtasks)
                        {
                            var subStatus = sub.IsFinished ? "[✓]" : "[ ]";
                            tc.Item().PaddingLeft(10).Text($"└─ {subStatus} {sub.Title}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        }
                    }
                });

                // Date Started
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(task.DateStarted.ToString("yyyy-MM-dd"));

                // Date Finished
                var dateFinishedText = task.DateFinished.HasValue ? task.DateFinished.Value.ToString("yyyy-MM-dd") : "-";
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(dateFinishedText);

                // Status
                var statusText = task.IsFinished ? "Finished" : "Pending";
                var statusColor = task.IsFinished ? Colors.Green.Darken2 : Colors.Orange.Darken2;
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignCenter().Text(statusText).Bold().FontColor(statusColor).FontSize(8);
            }
        }
    }
}
