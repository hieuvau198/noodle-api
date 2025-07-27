using System;
using System.Collections.Generic;

namespace UserService.Domain.Entities;

public partial class NoodleSpiceLevel
{
    public int NoodleSpiceLevelId { get; set; }

    public int NoodleId { get; set; }

    public int SpiceLevelId { get; set; }

    public int OrderItemId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SpicyNoodle Noodle { get; set; } = null!;

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual SpiceLevel SpiceLevel { get; set; } = null!;
}
