using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuditoryGenerator
{
    public partial class Events
    {
        public int? Id { get; set; }
        public DateTime? DateTime { get; set; }
        public int RoomId { get; set; }
        public string LessonRange { get; set; }
        public new string Name { get; set; }
    }
}
