using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Threading.Tasks;

public class ForgotPasswordModel : PageModel
{
    // Hardcoded connection string as you requested
    private readonly string _connectionString = "Data Source=DESKTOP-14GLE6P\\SQLEXPRESS;Initial Catalog=SchoolSysDB;Integrated Security=True;TrustServerCertificate=True";

    [BindProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    public string ResetLink { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Check if user exists
        var cmdCheck = new SqlCommand("SELECT Id FROM Users WHERE Email = @Email", connection);
        cmdCheck.Parameters.AddWithValue("@Email", Email);

        var userIdObj = await cmdCheck.ExecuteScalarAsync();

        if (userIdObj == null)
        {
            ModelState.AddModelError(string.Empty, "Email not found.");
            return Page();
        }

        int userId = (int)userIdObj;

        // Generate token and expiry
        string token = Guid.NewGuid().ToString();
        DateTime expiry = DateTime.Now.AddMinutes(30);

        // Update user record with token and expiry
        var cmdUpdate = new SqlCommand("UPDATE Users SET PasswordResetToken = @Token, TokenExpiry = @Expiry WHERE Id = @Id", connection);
        cmdUpdate.Parameters.AddWithValue("@Token", token);
        cmdUpdate.Parameters.AddWithValue("@Expiry", expiry);
        cmdUpdate.Parameters.AddWithValue("@Id", userId);

        await cmdUpdate.ExecuteNonQueryAsync();

        ResetLink = Url.Page("/ResetPassword", null, new { token = token }, Request.Scheme);

        // TODO: Send email with ResetLink instead of showing it

        return Page();
    }
}
