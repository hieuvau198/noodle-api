using System;
using System.Collections.Generic;

namespace UserService.Domain.Entities;

public partial class SpiceLevel
{
    public int SpiceLevelId { get; set; }

    public string Name { get; set; } = null!;

    public int Level { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<NoodleSpiceLevel> NoodleSpiceLevels { get; set; } = new List<NoodleSpiceLevel>();
}
