using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuditGeneratorAPI.Models;

public partial class TeacherGroup
{
    public int? Id { get; set; }

    public int GroupId { get; set; }

    public int TeacherId { get; set; }
    [JsonIgnore]
    public virtual Group Group { get; set; } = null!;
    [JsonIgnore]
    public virtual Teacher Teacher { get; set; } = null!;
}
