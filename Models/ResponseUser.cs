using System.ComponentModel.DataAnnotations;

namespace UserFactory.Models
{
    public class ResponseUser
    {
        [Required]
        [RegularExpression("^[A-Za-zА-Яа-яЁё]+$", ErrorMessage = "The name can only contain Latin and Russian letters")]
        [StringLength(30, ErrorMessage = "Name cannot be longer than 30 characters")]
        public string Name { get; set; } = default!;

        [Required(ErrorMessage = "The Gender field is required.")]
        public int? Gender { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        [Required]
        public bool IsActive {  get; set; }
    }
}
