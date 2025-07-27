using System.ComponentModel.DataAnnotations;

namespace OrderService.Domain;

public class OrderItem
{
    [Key]
    public int OrderItemId { get; set; }
    
    public int OrderId { get; set; }
    
    public int NoodleId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Subtotal must be non-negative")]
    public decimal Subtotal { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Order Order { get; set; } = null!;
    public virtual SpicyNoodle Noodle { get; set; } = null!;
} 