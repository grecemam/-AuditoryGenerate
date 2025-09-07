using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuditoryGenerator
{

    public class StudyPracticeViewModel
    {
        public int? Id { get; set; }
        public DateTime Date { get; set; }
        public int GroupId { get; set; }
        public int RoomId { get; set; }
        public int TeacherId { get; set; }
        public string LessonRange { get; set; }
        public string GroupName { get; set; }
        public string RoomNumber { get; set; }
        public string TeacherFullName { get; set; }
        public int PairStart
        {
            get
            {
                if (string.IsNullOrEmpty(LessonRange)) return 0;
                var parts = LessonRange.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int start)) return start;
                return 0;
            }
        }

        public int PairEnd
        {
            get
            {
                if (string.IsNullOrEmpty(LessonRange)) return 4;
                var parts = LessonRange.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out int end))return end;
                return 4;
            }
        }

    }

}
