using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using UserService2.Domain.Entities;

namespace UserService2.Domain.Context;

public partial class SpicyNoodleDbContext : DbContext
{
    public SpicyNoodleDbContext()
    {
    }

    public SpicyNoodleDbContext(DbContextOptions<SpicyNoodleDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
