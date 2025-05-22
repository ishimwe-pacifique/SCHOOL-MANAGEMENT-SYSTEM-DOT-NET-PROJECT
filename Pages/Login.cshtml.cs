using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using School_Management_System.Data;
using School_Management_System.Models;
using System.Net;
using System.Net.Mail;

namespace School_Management_System.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SchoolDBContext _context;
        private readonly IConfiguration _configuration;

        // For demo only: store 2FA codes in memory. Use DB in production.
        public static Dictionary<string, TwoFactorCode> TwoFACodes = new();

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string WelcomeMessage { get; set; }

        public LoginModel(SchoolDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(string.Empty, "Please fill in all required fields.");
                return Page();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == Email.ToLower());

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            var hashedPassword = HashPassword(Password);
            if (user.Password != hashedPassword)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            // Generate 2FA code
            string code = new Random().Next(100000, 999999).ToString();

            // Store the 2FA code with expiry in static dictionary
            TwoFACodes[Email] = new TwoFactorCode
            {
                Email = Email,
                Code = code,
                Expiry = DateTime.UtcNow.AddMinutes(5)
            };

            // Send 2FA code via email
            var emailSender = new EmailSender(_configuration);
            await emailSender.SendEmailAsync(Email, "Your 2FA Code", $"Your verification code is: {code}");

            // Store email and role temporarily for verification page
            TempData["PendingEmail"] = Email;
            TempData["Role"] = user.Role; // Get role from the user object

            // Redirect to 2FA verification page
            return RedirectToPage("/Verify2FA");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        // Helper class to hold 2FA code info
        public class TwoFactorCode
        {
            public string Email { get; set; }
            public string Code { get; set; }
            public DateTime Expiry { get; set; }
        }

        // Email sender helper class
        public class EmailSender
        {
            private readonly IConfiguration _config;

            public EmailSender(IConfiguration config)
            {
                _config = config;
            }

            public async Task SendEmailAsync(string toEmail, string subject, string message)
            {
                using var smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(
                        _config["EmailSettings:Username"],
                        _config["EmailSettings:Password"]),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage(_config["EmailSettings:Username"], toEmail, subject, message);
                await smtp.SendMailAsync(mailMessage);
            }
        }
    }
}