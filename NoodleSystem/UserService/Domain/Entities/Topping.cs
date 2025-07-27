using System;
using System.Collections.Generic;

namespace UserService.Domain.Entities;

public partial class Topping
{
    public int ToppingId { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<NoodleTopping> NoodleToppings { get; set; } = new List<NoodleTopping>();
}
