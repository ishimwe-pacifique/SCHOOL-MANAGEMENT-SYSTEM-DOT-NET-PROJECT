using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;

namespace School_Management_System.Pages
{
    [Authorize]
    public class TeachersManagementModel : PageModel
    {
        private readonly string connectionString = "Data Source=DESKTOP-14GLE6P\\SQLEXPRESS;Initial Catalog=SchoolSysDB;Integrated Security=True;TrustServerCertificate=True";
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        [BindProperty]
        public Teacher Teacher { get; set; }

        public List<TeacherDisplay> Teachers { get; set; } = new List<TeacherDisplay>();
        public List<SelectListItem> Subjects { get; set; } = new List<SelectListItem>();

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadTeachers();
            await LoadSubjects();
        }

        private async Task LoadTeachers()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"SELECT t.TeacherID, t.FullName, t.Email, t.Phone, 
                                t.PrimarySubjectID, s.SubjectName AS PrimarySubjectName, t.JoinDate 
                                FROM Teachers t
                                LEFT JOIN Subjects s ON t.PrimarySubjectID = s.SubjectID";

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Teachers.Add(new TeacherDisplay
                        {
                            TeacherID = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(2),
                            Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                            PrimarySubjectID = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            PrimarySubjectName = reader.IsDBNull(5) ? "N/A" : reader.GetString(5),
                            JoinDate = reader.GetDateTime(6)
                        });
                    }
                }
            }
        }

        private async Task LoadSubjects()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = @"SELECT s.SubjectID, s.SubjectName, c.ClassName
                            FROM Subjects s
                            INNER JOIN Classes c ON s.Classid = c.Classid";

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Subjects.Add(new SelectListItem
                        {
                            Value = reader.GetInt32(0).ToString(),
                            Text = $"{reader.GetString(1)} - {reader.GetString(2)}"
                        });
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostAddTeacherAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadTeachers();
                await LoadSubjects();
                return Page();
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Email check
                            var checkQuery = @"SELECT COUNT(*) FROM (
                                                SELECT Email FROM Teachers 
                                                UNION ALL 
                                                SELECT Email FROM Users
                                            ) AS Combined WHERE Email = @Email";

                            using (var checkCommand = new SqlCommand(checkQuery, connection, transaction))
                            {
                                checkCommand.Parameters.AddWithValue("@Email", Teacher.Email);
                                var exists = (int)await checkCommand.ExecuteScalarAsync();
                                if (exists > 0)
                                {
                                    ErrorMessage = "Email already exists in system";
                                    return RedirectToPage();
                                }
                            }

                            // Hash password
                            var hashedPassword = HashPassword(Teacher.TemporaryPassword);

                            // Create user
                            var userQuery = @"INSERT INTO Users 
                                            (FullName, Email, Password, Role, CreatedAt)
                                            VALUES (@FullName, @Email, @Password, 'Teacher', @CreatedAt)";

                            using (var userCommand = new SqlCommand(userQuery, connection, transaction))
                            {
                                userCommand.Parameters.AddWithValue("@FullName", Teacher.FullName);
                                userCommand.Parameters.AddWithValue("@Email", Teacher.Email);
                                userCommand.Parameters.AddWithValue("@Password", hashedPassword);
                                userCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                                await userCommand.ExecuteNonQueryAsync();
                            }

                            // Create teacher
                            var teacherQuery = @"INSERT INTO Teachers 
                                                (FullName, Email, Phone, PrimarySubjectID, JoinDate)
                                                VALUES (@FullName, @Email, @Phone, @PrimarySubjectID, @JoinDate)";

                            using (var teacherCommand = new SqlCommand(teacherQuery, connection, transaction))
                            {
                                teacherCommand.Parameters.AddWithValue("@FullName", Teacher.FullName);
                                teacherCommand.Parameters.AddWithValue("@Email", Teacher.Email);
                                teacherCommand.Parameters.AddWithValue("@Phone", Teacher.Phone ?? (object)DBNull.Value);
                                teacherCommand.Parameters.AddWithValue("@PrimarySubjectID",
                                    Teacher.PrimarySubjectID ?? (object)DBNull.Value);
                                teacherCommand.Parameters.AddWithValue("@JoinDate", Teacher.JoinDate);

                                await teacherCommand.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            SuccessMessage = "Teacher account created successfully";
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            ErrorMessage = $"Error creating teacher: {ex.Message}";
                            return RedirectToPage();
                        }
                    }
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"System error: {ex.Message}";
                return RedirectToPage();
            }
        }
        public async Task<IActionResult> OnPostEditTeacherAsync(int teacherId, string fullName, string email,
            string phone, int? primarySubjectID, DateTime joinDate)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var getEmailQuery = "SELECT Email FROM Teachers WHERE TeacherID = @TeacherID";
                            string oldEmail;

                            using (var getEmailCmd = new SqlCommand(getEmailQuery, connection, transaction))
                            {
                                getEmailCmd.Parameters.AddWithValue("@TeacherID", teacherId);
                                oldEmail = (string)await getEmailCmd.ExecuteScalarAsync();
                                if (string.IsNullOrEmpty(oldEmail))
                                {
                                    ErrorMessage = "Teacher not found";
                                    return RedirectToPage();
                                }
                            }

                            if (email != oldEmail)
                            {
                                var checkQuery = @"SELECT COUNT(*) FROM (
                                                    SELECT Email FROM Teachers 
                                                    UNION ALL 
                                                    SELECT Email FROM Users
                                                ) AS Combined WHERE Email = @Email";

                                using (var checkCmd = new SqlCommand(checkQuery, connection, transaction))
                                {
                                    checkCmd.Parameters.AddWithValue("@Email", email);
                                    var exists = (int)await checkCmd.ExecuteScalarAsync();
                                    if (exists > 0)
                                    {
                                        ErrorMessage = "New email is already in use";
                                        return RedirectToPage();
                                    }
                                }
                            }

                            var teacherQuery = @"UPDATE Teachers SET 
                                                FullName = @FullName,
                                                Email = @Email,
                                                Phone = @Phone,
                                                PrimarySubjectID = @PrimarySubjectID,
                                                JoinDate = @JoinDate
                                                WHERE TeacherID = @TeacherID";

                            using (var teacherCmd = new SqlCommand(teacherQuery, connection, transaction))
                            {
                                teacherCmd.Parameters.AddWithValue("@TeacherID", teacherId);
                                teacherCmd.Parameters.AddWithValue("@FullName", fullName);
                                teacherCmd.Parameters.AddWithValue("@Email", email);
                                teacherCmd.Parameters.AddWithValue("@Phone", phone ?? (object)DBNull.Value);
                                teacherCmd.Parameters.AddWithValue("@PrimarySubjectID",
                                    primarySubjectID ?? (object)DBNull.Value);
                                teacherCmd.Parameters.AddWithValue("@JoinDate", joinDate);

                                int affectedRows = await teacherCmd.ExecuteNonQueryAsync();
                                if (affectedRows == 0)
                                {
                                    throw new Exception("Teacher update failed");
                                }
                            }

                            var userQuery = @"UPDATE Users SET 
                                            FullName = @FullName,
                                            Email = @NewEmail
                                            WHERE Email = @OldEmail";

                            using (var userCmd = new SqlCommand(userQuery, connection, transaction))
                            {
                                userCmd.Parameters.AddWithValue("@NewEmail", email);
                                userCmd.Parameters.AddWithValue("@FullName", fullName);
                                userCmd.Parameters.AddWithValue("@OldEmail", oldEmail);
                                await userCmd.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            SuccessMessage = "Teacher updated successfully!";
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            ErrorMessage = "Error updating teacher: " + ex.Message;
                            return RedirectToPage();
                        }
                    }
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ErrorMessage = "System error: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteTeacher(int teacherId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var getEmailQuery = "SELECT Email FROM Teachers WHERE TeacherID = @TeacherID";
                            string email;

                            using (var getEmailCmd = new SqlCommand(getEmailQuery, connection, transaction))
                            {
                                getEmailCmd.Parameters.AddWithValue("@TeacherID", teacherId);
                                email = (string)await getEmailCmd.ExecuteScalarAsync();
                                if (string.IsNullOrEmpty(email))
                                {
                                    ErrorMessage = "Teacher not found";
                                    return RedirectToPage();
                                }
                            }

                            var deleteTeacherQuery = "DELETE FROM Teachers WHERE TeacherID = @TeacherID";
                            using (var teacherCmd = new SqlCommand(deleteTeacherQuery, connection, transaction))
                            {
                                teacherCmd.Parameters.AddWithValue("@TeacherID", teacherId);
                                int affectedRows = await teacherCmd.ExecuteNonQueryAsync();
                                if (affectedRows == 0)
                                {
                                    throw new Exception("Teacher delete failed");
                                }
                            }

                            var deleteUserQuery = "DELETE FROM Users WHERE Email = @Email";
                            using (var userCmd = new SqlCommand(deleteUserQuery, connection, transaction))
                            {
                                userCmd.Parameters.AddWithValue("@Email", email);
                                await userCmd.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();
                            SuccessMessage = "Teacher and user account deleted successfully!";
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            ErrorMessage = "Error deleting teacher: " + ex.Message;
                            return RedirectToPage();
                        }
                    }
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ErrorMessage = "System error: " + ex.Message;
                return RedirectToPage();
            }
        }
    }

    public class Teacher
    {
        public int TeacherID { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Temporary password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be 8-100 characters")]
        public string TemporaryPassword { get; set; }

        [Display(Name = "Primary Subject")]
        public int? PrimarySubjectID { get; set; }

        [Required(ErrorMessage = "Join date is required")]
        [DataType(DataType.Date)]
        public DateTime JoinDate { get; set; }
    }
    public class TeacherDisplay
    {
        public int TeacherID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int? PrimarySubjectID { get; set; }
        public string PrimarySubjectName { get; set; }
        public DateTime JoinDate { get; set; }
    }

    public class Subject
    {
        public int SubjectID { get; set; }

        [Required]
        public string SubjectName { get; set; }

        public string SubjectCode { get; set; }
        public int? ClassId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
