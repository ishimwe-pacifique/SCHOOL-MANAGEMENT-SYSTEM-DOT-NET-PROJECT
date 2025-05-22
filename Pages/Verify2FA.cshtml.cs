using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace School_Management_System.Pages
{
    public class Verify2FAModel : PageModel
    {
        [BindProperty]
        public string Code { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Just display the page
            if (TempData["PendingEmail"] == null || TempData["Role"] == null)
            {
                return RedirectToPage("/Login");
            }

            // Keep TempData for next request (POST)
            TempData.Keep("PendingEmail");
            TempData.Keep("Role");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var email = TempData["PendingEmail"] as string;
            var role = TempData["Role"] as string;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(role))
            {
                ModelState.AddModelError(string.Empty, "Session expired. Please login again.");
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrWhiteSpace(Code))
            {
                ModelState.AddModelError(string.Empty, "Please enter the 2FA code.");
                TempData.Keep("PendingEmail");
                TempData.Keep("Role");
                return Page();
            }

            if (!LoginModel.TwoFACodes.TryGetValue(email, out var twoFactorCode) ||
                twoFactorCode.Code != Code ||
                twoFactorCode.Expiry < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired 2FA code.");
                TempData.Keep("PendingEmail");
                TempData.Keep("Role");
                return Page();
            }

            // Sign in user with claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email), // Or full name if you want to fetch it
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            // Remove used 2FA code
            LoginModel.TwoFACodes.Remove(email);

            // Clear TempData
            TempData.Remove("PendingEmail");
            TempData.Remove("Role");

            // Redirect based on role
            return role.ToLower() switch
            {
                "admin" => RedirectToPage("/AdminDashboard"),
                "teacher" => RedirectToPage("/TeacherDashboard"),
                _ => RedirectToPage("/AdminDashboard")
            };
        }
    }
}
