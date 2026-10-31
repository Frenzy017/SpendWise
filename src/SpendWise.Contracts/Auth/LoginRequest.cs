using System.ComponentModel.DataAnnotations;

namespace SpendWise.Contracts.Auth;

public record LoginRequest([Required] string Username, [Required] string Password);
