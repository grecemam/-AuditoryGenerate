using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuditGeneratorAPI.Models;

public partial class Qualification
{
    public int? Id { get; set; }

    public string Code { get; set; } = null!;

    public string Qualification1 { get; set; } = null!;
    [JsonIgnore]
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
}
