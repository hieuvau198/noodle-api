using System.ComponentModel.DataAnnotations;

namespace PaymentService.Domain;

public class Payment
{
    [Key]
    public int PaymentId { get; set; }
    
    public int OrderId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending";
    
    [StringLength(50)]
    public string? PaymentMethod { get; set; }
    
    [StringLength(255)]
    public string? TransactionId { get; set; }
    
    public DateTime? PaidAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
} 