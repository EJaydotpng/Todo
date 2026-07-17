using System;
using System.Collections.Generic;

namespace TodoApp.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public DateTime DateStarted { get; set; } = DateTime.Now;
        public DateTime? DateFinished { get; set; }
        public bool IsFinished { get; set; }
        public string? SubCategory { get; set; }

        // Self-referencing relationship for subtasks
        public int? ParentTaskId { get; set; }
        public TaskItem? ParentTask { get; set; }
        public ICollection<TaskItem> Subtasks { get; set; } = new List<TaskItem>();
    }
}
