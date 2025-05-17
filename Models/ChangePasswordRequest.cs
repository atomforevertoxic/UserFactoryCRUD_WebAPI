using System.ComponentModel.DataAnnotations;

namespace UserFactory.Models
{
    public class ChangePasswordRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; }
    }
}
