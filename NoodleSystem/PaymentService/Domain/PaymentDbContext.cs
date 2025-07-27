using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;

namespace PaymentService.Domain;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);
            
            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.TransactionId).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            
            // Create index on OrderId for better performance
            entity.HasIndex(e => e.OrderId).HasDatabaseName("IX_Payments_OrderId");
        });
    }
} 