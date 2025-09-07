using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuditoryGenerator
{
    public class CombinedSpecialty
    {
        public string Header { get; set; }
        public List<(string groupName, int year)> Groups { get; set; } = new List<(string groupName, int year)>();
    }
}
