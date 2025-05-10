using System.ComponentModel.DataAnnotations;

namespace UserFactory.Models
{
    public class LoginViewModel
    {
        [Required]
        public string Login { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;
    }
}
