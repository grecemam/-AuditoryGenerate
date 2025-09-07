using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuditGeneratorAPI.Models;

public partial class Event
{
    public int? Id { get; set; }

    public DateTime Date { get; set; }

    public string Name { get; set; } = null!;

    public int RoomId { get; set; }

    public string? LessonRange { get; set; }
}
