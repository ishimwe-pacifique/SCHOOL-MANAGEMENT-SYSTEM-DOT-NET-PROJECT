using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using School_Management_System.Data;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Models;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace School_Management_System.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SchoolDBContext _context;

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

       

        public string WelcomeMessage { get; set; }

        public LoginModel(SchoolDBContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
           

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == Email );

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            var hashedPassword = HashPassword(Password);
            if (user.Password != hashedPassword)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            // Redirect based on role
            if (user.Role == "Admin")
            {
                return RedirectToPage("/AdminDashboard");
            }
            else if (user.Role == "Teacher")
            {
                return RedirectToPage("/TeacherDashboard"); // You'll need to create this
            }

            return RedirectToPage("/Index");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}