using System.ComponentModel.DataAnnotations;

namespace UserFactory.Models
{
    public class UpdateProfileRequest
    {
        [Required]
        [RegularExpression("^[A-Za-zА-Яа-яЁё]+$", ErrorMessage = "The name can only contain Latin and Russian letters")]
        [StringLength(30, ErrorMessage = "Name cannot be longer than 30 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "The Gender field is required.")]
        [Range(0, 2)]
        public int? Gender { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }
    }
}
