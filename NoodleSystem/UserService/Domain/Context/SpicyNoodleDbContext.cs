using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;

namespace UserService.Domain.Context;

public partial class SpicyNoodleDbContext : DbContext
{
    public SpicyNoodleDbContext()
    {
    }

    public SpicyNoodleDbContext(DbContextOptions<SpicyNoodleDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<NoodleSpiceLevel> NoodleSpiceLevels { get; set; }

    public virtual DbSet<NoodleTopping> NoodleToppings { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<SpiceLevel> SpiceLevels { get; set; }

    public virtual DbSet<SpicyNoodle> SpicyNoodles { get; set; }

    public virtual DbSet<Topping> Toppings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=SpicyNoodleDB;Integrated Security=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NoodleSpiceLevel>(entity =>
        {
            entity.HasKey(e => e.NoodleSpiceLevelId).HasName("PK__NoodleSp__BCE079CAE896A61C");

            entity.HasIndex(e => new { e.NoodleId, e.SpiceLevelId, e.OrderItemId }, "UQ__NoodleSp__CD7EBD9D21EA0AB3").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Noodle).WithMany(p => p.NoodleSpiceLevels)
                .HasForeignKey(d => d.NoodleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NoodleSpi__Noodl__60A75C0F");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.NoodleSpiceLevels)
                .HasForeignKey(d => d.OrderItemId)
                .HasConstraintName("FK__NoodleSpi__Order__628FA481");

            entity.HasOne(d => d.SpiceLevel).WithMany(p => p.NoodleSpiceLevels)
                .HasForeignKey(d => d.SpiceLevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NoodleSpi__Spice__619B8048");
        });

        modelBuilder.Entity<NoodleTopping>(entity =>
        {
            entity.HasKey(e => e.NoodleToppingId).HasName("PK__NoodleTo__228F6224F0EDC434");

            entity.HasIndex(e => new { e.NoodleId, e.ToppingId, e.OrderItemId }, "UQ__NoodleTo__1223F209FC92B099").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Noodle).WithMany(p => p.NoodleToppings)
                .HasForeignKey(d => d.NoodleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NoodleTop__Noodl__59FA5E80");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.NoodleToppings)
                .HasForeignKey(d => d.OrderItemId)
                .HasConstraintName("FK__NoodleTop__Order__5BE2A6F2");

            entity.HasOne(d => d.Topping).WithMany(p => p.NoodleToppings)
                .HasForeignKey(d => d.ToppingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NoodleTop__Toppi__5AEE82B9");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCF16E0FCB7");

            entity.HasIndex(e => e.Status, "IX_Orders_Status");

            entity.HasIndex(e => e.UserId, "IX_Orders_UserId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__UserId__4F7CD00D");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED068110B24B15");

            entity.HasIndex(e => e.NoodleId, "IX_OrderItems_NoodleId");

            entity.HasIndex(e => e.OrderId, "IX_OrderItems_OrderId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Noodle).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.NoodleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderItem__Noodl__5535A963");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderItem__Order__5441852A");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38A15DFC88");

            entity.HasIndex(e => e.OrderId, "IX_Payments_OrderId");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TransactionId).HasMaxLength(255);

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__OrderI__6754599E");
        });

        modelBuilder.Entity<SpiceLevel>(entity =>
        {
            entity.HasKey(e => e.SpiceLevelId).HasName("PK__SpiceLev__1BD635C8E8F8990E");

            entity.HasIndex(e => e.Level, "UQ__SpiceLev__AAF89962EFDF150E").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<SpicyNoodle>(entity =>
        {
            entity.HasKey(e => e.NoodleId).HasName("PK__SpicyNoo__DC9433C689A3A2EC");

            entity.Property(e => e.BasePrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Topping>(entity =>
        {
            entity.HasKey(e => e.ToppingId).HasName("PK__Toppings__EE02CC851EDC9169");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C7D0F32DD");

            entity.HasIndex(e => e.Email, "IX_Users_Email");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534AABD56C7").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.GoogleId).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.Role).HasDefaultValue(2);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
