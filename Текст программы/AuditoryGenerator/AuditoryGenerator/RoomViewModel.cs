using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuditoryGenerator
{
    public class RoomViewModel
    {
        public int? Id { get; set; }
        public string RoomNumber { get; set; }
        public int CampusId { get; set; }
        public string CampusName { get; set; }
    }
}
