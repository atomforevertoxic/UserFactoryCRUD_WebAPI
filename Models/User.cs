﻿using System;
using System.ComponentModel.DataAnnotations;

namespace UserFactory.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid(); 

        [Required]
        [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "Login can contain only Latin letters and numbers")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Login must be between 3 and 30 characters")]
        public string Login { get; set; }

        [Required]
        [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "Password can contain only Latin letters and numbers")]
        [StringLength(30, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Required]
        [RegularExpression("^[A-Za-zА-Яа-яЁё]+$", ErrorMessage = "The name can only contain Latin and Russian letters")]
        [StringLength(30, ErrorMessage = "Name cannot be longer than 30 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "The Gender field is required.")]
        public int? Gender { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        public bool Admin { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }

        public DateTime? RevokedOn { get; set; }
        public string? RevokedBy { get; set; }
    }
}