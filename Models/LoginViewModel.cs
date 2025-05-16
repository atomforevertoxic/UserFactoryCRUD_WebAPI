using System.ComponentModel.DataAnnotations;

namespace UserFactory.Models
{
    public class LoginViewModel
    {
        [Required]
        [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "Login can contain only Latin letters and numbers")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Login must be between 3 and 30 characters")]
        public string Login { get; set; } = default!;

        [Required]
        [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "Password can contain only Latin letters and numbers")]
        [StringLength(30, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = default!;
    }
}
