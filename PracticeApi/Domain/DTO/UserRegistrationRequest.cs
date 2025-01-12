using System.ComponentModel.DataAnnotations;

public class UserRegistrationRequest
{
    [Required]
    public string Name { get; set; }


    [Required]
    public string UserName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\W).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one special character.")]
    public string Password { get; set; }

    [Required]
    public string ConfirmPassword { get; set; }


    [Required]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be 11 digits.")]
    public string PhoneNumber { get; set; }

}
