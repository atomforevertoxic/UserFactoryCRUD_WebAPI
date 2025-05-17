using System.ComponentModel.DataAnnotations;

namespace UserFactory.Models
{
    public class ChangePasswordRequest
    {
        [Required]
        [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "Password can contain only Latin letters and numbers")]
        [StringLength(30, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; }
    }
}
