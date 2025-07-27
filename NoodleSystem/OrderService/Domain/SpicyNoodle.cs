using System.ComponentModel.DataAnnotations;

namespace OrderService.Domain;

public class SpicyNoodle
{
    [Key]
    public int NoodleId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Base price must be non-negative")]
    public decimal BasePrice { get; set; }
    
    [StringLength(500)]
    public string? ImageUrl { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
} 