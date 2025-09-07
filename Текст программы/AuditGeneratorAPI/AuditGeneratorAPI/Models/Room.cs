using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuditGeneratorAPI.Models;

public partial class Room
{
    public int? Id { get; set; }

    public string RoomNumber { get; set; } = null!;

    public int CampusId { get; set; }
    [JsonIgnore]
    public virtual ICollection<AssignedRoom> AssignedRooms { get; set; } = new List<AssignedRoom>();
    [JsonIgnore]
    public virtual Campus? Campus { get; set; } = null!;
    [JsonIgnore]
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    [JsonIgnore]
    public virtual ICollection<RoomSchedule> RoomSchedules { get; set; } = new List<RoomSchedule>();
    [JsonIgnore]
    public virtual ICollection<StudyPractice> StudyPractices { get; set; } = new List<StudyPractice>();
}
