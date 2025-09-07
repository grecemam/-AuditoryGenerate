using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuditGeneratorAPI.Models;

public partial class AssignedRoom
{
    public int? Id { get; set; }

    public int RoomId { get; set; }

    public int TeacherId { get; set; }
    public virtual Room? Room { get; set; }
    public virtual Teacher? Teacher { get; set; }
}
