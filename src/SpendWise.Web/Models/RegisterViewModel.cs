using System.ComponentModel.DataAnnotations;
using SpendWise.Contracts;

namespace SpendWise.Web.Models;

public class RegisterViewModel
{
    [Required, StringLength(ValidationLimits.UsernameMaxLength, MinimumLength = ValidationLimits.UsernameMinLength),
     RegularExpression(ValidationLimits.UsernamePattern, ErrorMessage = "Only letters, digits, '_' and '.' are allowed.")]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(ValidationLimits.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password),
     StringLength(ValidationLimits.PasswordMaxLength, MinimumLength = ValidationLimits.PasswordMinLength)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Confirm password"),
     Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
