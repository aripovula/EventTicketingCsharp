using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [AllowedValues("user", "admin")]
    public string Role { get; set; } = "user";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
