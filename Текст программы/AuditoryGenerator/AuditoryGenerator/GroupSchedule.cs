using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuditoryGenerator
{
    public class GroupSchedule
    {
        [JsonProperty("Groups")]
        public Dictionary<string, GroupData> Groups { get; set; } = new Dictionary<string, GroupData>();
    }
    public class GroupData
    {
        [JsonProperty("Days")]
        public Dictionary<string, DayData> Days { get; set; } = new Dictionary<string, DayData>();

        public static implicit operator List<object>(GroupData v)
        {
            throw new NotImplementedException();
        }
    }
    public class DayData
    {
        [JsonProperty("Buildings")]
        public Dictionary<string, List<Lesson>> Buildings { get; set; } = new Dictionary<string, List<Lesson>>();
    }
    public class Lesson
    {
        [JsonProperty("LessonNumber")]
        public string LessonNumber { get; set; } = string.Empty;

        [JsonProperty("Subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonProperty("Teacher")]
        public string Teacher { get; set; } = string.Empty;
    }
}
