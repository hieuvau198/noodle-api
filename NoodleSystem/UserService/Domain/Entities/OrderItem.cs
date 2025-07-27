using System;
using System.Collections.Generic;

namespace UserService.Domain.Entities;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int NoodleId { get; set; }

    public int Quantity { get; set; }

    public decimal Subtotal { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SpicyNoodle Noodle { get; set; } = null!;

    public virtual ICollection<NoodleSpiceLevel> NoodleSpiceLevels { get; set; } = new List<NoodleSpiceLevel>();

    public virtual ICollection<NoodleTopping> NoodleToppings { get; set; } = new List<NoodleTopping>();

    public virtual Order Order { get; set; } = null!;
}
