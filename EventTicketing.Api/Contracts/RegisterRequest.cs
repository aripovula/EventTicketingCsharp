using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Contracts;

public record RegisterRequest(
    [Required, StringLength(100, MinimumLength = 2)] string Name,
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, MinLength(8)] string Password);
