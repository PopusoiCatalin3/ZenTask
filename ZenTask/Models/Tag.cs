﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenTask.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ColorHex { get; set; }
        public int UserId { get; set; }
    }
}
