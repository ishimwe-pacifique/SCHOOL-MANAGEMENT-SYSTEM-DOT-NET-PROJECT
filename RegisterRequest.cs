using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystems.Models
{
    public class RegisterRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Role { get; set; }
    }
}
