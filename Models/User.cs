using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; }

        [NotMapped]
        public string WelcomeMessage { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? TokenExpiry { get; set; }


    }
}
