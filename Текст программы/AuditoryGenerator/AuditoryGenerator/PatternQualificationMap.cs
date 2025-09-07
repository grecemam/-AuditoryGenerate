using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuditoryGenerator
{
    public class PatternQualificationMap
    {
        public string Pattern { get; set; }
        public string QualificationCode { get; set; }
        public List<string> MergedPatterns { get; set; } = new List<string>();
    }
}
