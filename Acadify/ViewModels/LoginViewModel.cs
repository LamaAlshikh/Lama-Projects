using System.ComponentModel.DataAnnotations;

namespace Acadify.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        // Students do not select a role.
        // Staff members select Admin or Advisor.
        public string? Role { get; set; }
    }
}