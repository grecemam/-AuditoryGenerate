using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuditGeneratorAPI.Models;

public partial class Teacher
{
    public int? Id { get; set; }

    public string FullName { get; set; } = null!;
    [JsonIgnore]
    public virtual ICollection<AssignedRoom> AssignedRooms { get; set; } = new List<AssignedRoom>();
    [JsonIgnore]
    public virtual ICollection<TeacherGroup> TeacherGroups { get; set; } = new List<TeacherGroup>();
}
