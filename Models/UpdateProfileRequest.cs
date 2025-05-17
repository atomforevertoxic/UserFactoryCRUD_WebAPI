using System.ComponentModel.DataAnnotations;

namespace UserFactory.Models
{
    public class UpdateProfileRequest
    {
        [StringLength(50)]
        public string Name { get; set; }

        [Range(0, 2)]
        public int? Gender { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }
    }
}
