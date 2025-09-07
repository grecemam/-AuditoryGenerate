using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuditoryGenerator
{
    public class EventsViewModel
    {
        public int? Id { get; set; }
        public DateTime Date { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; }
        public int CampusId { get; set; }
        public string LessonRange { get; set; }
        public string Name { get; set; }
    }
}
