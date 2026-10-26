using System.ComponentModel.DataAnnotations;

namespace FiscApi.DTOs;

/// <summary>Body for POST /api/auth/login.</summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(100)]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(256)]
    public string? Password { get; set; }
}
