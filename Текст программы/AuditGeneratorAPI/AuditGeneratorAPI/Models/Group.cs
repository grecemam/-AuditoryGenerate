using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuditGeneratorAPI.Models;

public partial class Group
{
    public int? Id { get; set; }

    public string Name { get; set; } = null!;

    public string Abbreviation { get; set; } = null!;

    public int YearOfAdmission { get; set; }

    public int QualificationId { get; set; }

    public string? AssignedCells { get; set; }

    /*public virtual Qualification? Qualification { get; set; }

    public virtual ICollection<RoomSchedule> RoomSchedules { get; set; }


    public virtual ICollection<TeacherGroup> TeacherGroups { get; set; }

    public virtual ICollection<StudyPractice> StudyPractices { get; set; }*/
}
