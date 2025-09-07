using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuditoryGenerator
{
    class ChangeEntry
    {
        public string GroupName;
        public int PairNumber;
        public List<string> Teachers;
        public string RawText { get; set; }
    }

}
