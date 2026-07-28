using System.Collections.Generic;

namespace TodoApp.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Extracted first letter for collapsed sidebar badges (Slack/Discord style)
        public string Initial => string.IsNullOrWhiteSpace(Name) ? string.Empty : Name.Substring(0, 1).ToUpper();

        // Navigation property
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}