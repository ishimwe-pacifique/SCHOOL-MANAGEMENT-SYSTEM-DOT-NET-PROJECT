using System.Text;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystems.Models;
using System.Security.Cryptography;
using SchoolManagementSystems.Data;
using Microsoft.EntityFrameworkCore;

namespace SchoolManagementSystems.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SchoolDbContext _context;

        public AuthController(SchoolDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.Role == request.Role);

            if (user == null || user.Password != HashPassword(request.Password))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Invalid login attempt"
                });
            }

            return Ok(new ApiResponse
            {
                Success = true,
                Message = $"Welcome {user.FullName}!",
                Data = new { user.Email, user.Role }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request.Password != request.ConfirmPassword)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Passwords do not match"
                });
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return Conflict(new ApiResponse
                {
                    Success = false,
                    Message = "Email already registered"
                });
            }

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = HashPassword(request.Password),
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Registration successful"
            });
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
