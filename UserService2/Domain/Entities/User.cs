using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UserService2.Domain.Entities;

public partial class User
{
    [Key]
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Password { get; set; }

    public string? GoogleId { get; set; }

    public int Role { get; set; }

    public bool IsGoogleUser { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
