using System;
using System.Collections.Generic;
using System.Text;

namespace NumberService
{
    public class ConflictResult
    {
        public NumberResult Current { get; set; }
        public NumberResult Conflict { get; set; }
    }
}
