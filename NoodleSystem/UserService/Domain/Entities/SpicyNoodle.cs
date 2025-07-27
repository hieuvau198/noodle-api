using System;
using System.Collections.Generic;

namespace UserService.Domain.Entities;

public partial class SpicyNoodle
{
    public int NoodleId { get; set; }

    public string Name { get; set; } = null!;

    public decimal BasePrice { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<NoodleSpiceLevel> NoodleSpiceLevels { get; set; } = new List<NoodleSpiceLevel>();

    public virtual ICollection<NoodleTopping> NoodleToppings { get; set; } = new List<NoodleTopping>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
