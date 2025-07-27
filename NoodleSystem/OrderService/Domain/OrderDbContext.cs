using Microsoft.EntityFrameworkCore;
using OrderService.Domain;

namespace OrderService.Domain;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<SpicyNoodle> SpicyNoodles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10, 2)").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            
            // Create indexes for better performance
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_Orders_UserId");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_Orders_Status");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId);
            
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            
            // Create indexes for better performance
            entity.HasIndex(e => e.OrderId).HasDatabaseName("IX_OrderItems_OrderId");
            entity.HasIndex(e => e.NoodleId).HasDatabaseName("IX_OrderItems_NoodleId");
            
            // Configure relationships
            entity.HasOne(d => d.Order)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(d => d.Noodle)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.NoodleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SpicyNoodle>(entity =>
        {
            entity.HasKey(e => e.NoodleId);
            
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.BasePrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
        });
    }
} 