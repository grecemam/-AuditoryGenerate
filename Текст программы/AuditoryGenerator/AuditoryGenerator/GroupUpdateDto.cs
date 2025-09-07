using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuditoryGenerator
{
    public class GroupUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public int YearOfAdmission { get; set; }
        public int QualificationId { get; set; }
        public string AssignedCells { get; set; }
    }

}
