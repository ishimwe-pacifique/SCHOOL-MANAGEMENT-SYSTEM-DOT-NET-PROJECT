using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class ResetPasswordModel : PageModel
{
    private readonly string _connectionString;

    public ResetPasswordModel()
    {
        _connectionString = "Data Source=DESKTOP-14GLE6P\\SQLEXPRESS;Initial Catalog=SchoolSysDB;Integrated Security=True;TrustServerCertificate=True";
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; }

    [BindProperty]
    public string NewPassword { get; set; }

    [BindProperty]
    public string ConfirmPassword { get; set; }

    public string Message { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Token))
        {
            return RedirectToPage("/ForgotPassword");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ModelState.AddModelError(string.Empty, "Both password fields are required.");
            return Page();
        }

        if (NewPassword.Trim() != ConfirmPassword.Trim())
        {
            ModelState.AddModelError(string.Empty, "Passwords do not match.");
            return Page();
        }

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = new SqlCommand("SELECT Id, TokenExpiry FROM Users WHERE PasswordResetToken = @Token", connection);
        cmd.Parameters.AddWithValue("@Token", Token);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            ModelState.AddModelError(string.Empty, "Invalid or expired token.");
            return Page();
        }

        var expiry = reader.GetDateTime(reader.GetOrdinal("TokenExpiry"));
        if (expiry < DateTime.Now)
        {
            ModelState.AddModelError(string.Empty, "Reset token has expired.");
            return Page();
        }

        int userId = reader.GetInt32(reader.GetOrdinal("Id"));
        await reader.CloseAsync();

        // Hash password using SHA256 (for demonstration only – use a stronger hash like BCrypt in production)
        string hashedPassword = HashPassword(NewPassword.Trim());

        var cmdUpdate = new SqlCommand(@"
            UPDATE Users 
            SET Password = @Password, 
                PasswordResetToken = NULL, 
                TokenExpiry = NULL 
            WHERE Id = @Id", connection);

        cmdUpdate.Parameters.AddWithValue("@Password", hashedPassword);
        cmdUpdate.Parameters.AddWithValue("@Id", userId);

        await cmdUpdate.ExecuteNonQueryAsync();

        return RedirectToPage("/ResetSuccess");
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
