using System.ComponentModel.DataAnnotations;

namespace UserFactory.Models
{
    public class UserProfileResponse
    {
        [Required]
        [RegularExpression("^[A-Za-zА-Яа-яЁё]+$", ErrorMessage = "The name can only contain Latin and Russian letters")]
        [StringLength(30, ErrorMessage = "Name cannot be longer than 30 characters")]
        public string Name { get; set; } = default!;

        [Required(ErrorMessage = "The Gender field is required.")]
        [Range(0, 2)]
        public int? Gender { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        [Required]
        public bool IsActive {  get; set; }


        public UserProfileResponse(string name, int? gender, DateTime? birthday, bool isActive)        
        {
            Name = name;
            Gender = gender;
            Birthday = birthday;
            IsActive = isActive;
        }

    }
}
