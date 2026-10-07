using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Api.Contracts;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
