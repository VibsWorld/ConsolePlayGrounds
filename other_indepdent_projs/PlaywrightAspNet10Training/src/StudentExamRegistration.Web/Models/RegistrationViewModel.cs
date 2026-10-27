using System.ComponentModel.DataAnnotations;

namespace StudentExamRegistration.Web.Models;

public class RegistrationViewModel
{
    [Required(ErrorMessage = "Name is required.")]
    [Display(Name = "Full Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    public List<string> Courses { get; set; } = new();

    [Required(ErrorMessage = "Receipt id is required.")]
    [Display(Name = "Receipt / Transaction Id")]
    public string ReceiptId { get; set; } = string.Empty;
}