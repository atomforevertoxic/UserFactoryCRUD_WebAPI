using System.ComponentModel.DataAnnotations;

namespace UserFactory.Models
{
    public class ChangeLoginRequest
    {
        [Required]
        [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "Login can contain only Latin letters and numbers")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Login must be between 3 and 30 characters")]
        public string NewLogin { get; set; }
    }
}
