using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuditGeneratorAPI.Models;

public partial class Campus
{
    public int? Id { get; set; }

    public string Name { get; set; } = null!;
    [JsonIgnore]
    public virtual ICollection<RoomSchedule> RoomSchedules { get; set; } = new List<RoomSchedule>();
    [JsonIgnore]
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
