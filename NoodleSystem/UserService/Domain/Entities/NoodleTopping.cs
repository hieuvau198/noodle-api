using System;
using System.Collections.Generic;

namespace UserService.Domain.Entities;

public partial class NoodleTopping
{
    public int NoodleToppingId { get; set; }

    public int NoodleId { get; set; }

    public int ToppingId { get; set; }

    public int OrderItemId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SpicyNoodle Noodle { get; set; } = null!;

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual Topping Topping { get; set; } = null!;
}
