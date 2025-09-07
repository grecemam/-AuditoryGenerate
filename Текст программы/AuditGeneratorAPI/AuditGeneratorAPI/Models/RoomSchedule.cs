using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuditGeneratorAPI.Models;

public partial class RoomSchedule
{
    public int? Id { get; set; }

    public int GroupId { get; set; }

    public int RoomId { get; set; }

    public int LessonNumber { get; set; }

    public int WeekTypeId { get; set; }

    public int WeekdayId { get; set; }

    public int CampusId { get; set; }
    [JsonIgnore]
    public virtual Campus Campus { get; set; } = null!;
    [JsonIgnore]
    public virtual Group Group { get; set; } = null!;
    [JsonIgnore]
    public virtual Room Room { get; set; } = null!;
    [JsonIgnore]
    public virtual WeekType WeekType { get; set; } = null!;
    [JsonIgnore]
    public virtual Weekday Weekday { get; set; } = null!;
}
