using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenTask.Models
{
    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    public enum TaskStatus
    {
        ToDo,
        InProgress,
        Completed
    }

    public class Task
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskStatus Status { get; set; }
        public int CategoryId { get; set; }
        public bool IsRecurring { get; set; }
        public string RecurrencePattern { get; set; }
        public int EstimatedDuration { get; set; } 
        public List<int> TagIds { get; set; } = new List<int>();
        public List<SubTask> SubTasks { get; set; } = new List<SubTask>();
        public int UserId { get; set; }

        public Task()
        {
            CreatedDate = DateTime.Now;
            Status = TaskStatus.ToDo;
            Priority = TaskPriority.Medium;
        }
    }
}
