using System.ComponentModel.DataAnnotations;

namespace OrderService.Domain;

public class Order
{
    [Key]
    public int OrderId { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending";
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Total amount must be non-negative")]
    public decimal TotalAmount { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
} 