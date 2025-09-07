using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuditGeneratorAPI.Models;


public partial class StudyPractice
{
    public int? Id { get; set; }

    public DateTime Date { get; set; }

    public int GroupId { get; set; }

    public int RoomId { get; set; }

    public int TeacherId { get; set; }

    public string? LessonRange { get; set; }
}

