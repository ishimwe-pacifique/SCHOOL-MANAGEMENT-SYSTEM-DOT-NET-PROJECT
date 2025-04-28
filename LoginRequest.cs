using System.ComponentModel.DataAnnotations;

namespace SchoolManagementSystems.Models
{
    public class LoginRequest
    {
        public string Email { get; set; }
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public string Role { get; set; }
    }
}
