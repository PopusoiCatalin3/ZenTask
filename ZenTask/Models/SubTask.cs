using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenTask.Models
{
    public class SubTask
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
        public int DisplayOrder { get; set; }
    }
}
