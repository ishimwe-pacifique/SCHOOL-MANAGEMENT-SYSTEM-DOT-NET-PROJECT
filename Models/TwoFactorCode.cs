using System;

namespace School_Management_System.Models
{
    public class TwoFactorCode
    {
        public string Email { get; set; } = "";
        public string Code { get; set; } = "";
        public DateTime Expiry { get; set; }
    }
}
