using System.ComponentModel.DataAnnotations;

namespace SpendWise.Contracts.Auth;

// The "at least one letter and one digit" rule is a business rule checked in AuthService.
public record RegisterRequest(
    [Required, StringLength(ValidationLimits.UsernameMaxLength, MinimumLength = ValidationLimits.UsernameMinLength),
     RegularExpression(ValidationLimits.UsernamePattern, ErrorMessage = "Username may contain only letters, digits, '_' and '.'.")]
    string Username,
    [Required, EmailAddress, StringLength(ValidationLimits.EmailMaxLength)]
    string Email,
    [Required, StringLength(ValidationLimits.PasswordMaxLength, MinimumLength = ValidationLimits.PasswordMinLength)]
    string Password);
