using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using School_Management_System.Pages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace School_Management_System.Pages
{
    public class TeachersManagementModel : PageModel
    {
        private readonly string connectionString = "Data Source=DESKTOP-14GLE6P\\SQLEXPRESS;Initial Catalog=SchoolSysDB;Integrated Security=True;TrustServerCertificate=True";

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
                var command = new SqlCommand("SELECT SubjectID, SubjectName FROM Subjects", connection);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Subjects.Add(new SelectListItem
                        {
                            Value = reader.GetInt32(0).ToString(),
                            Text = reader.GetString(1)
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
                    var checkEmailQuery = "SELECT COUNT(*) FROM Teachers WHERE Email = @Email";
                    using (var checkCommand = new SqlCommand(checkEmailQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", Teacher.Email);
                        var exists = (int)await checkCommand.ExecuteScalarAsync();
                        if (exists > 0)
                        {
                            ErrorMessage = "A teacher with this email already exists.";
                            return RedirectToPage();
                        }
                    }

                    string query = @"INSERT INTO Teachers 
                                    (FullName, Email, Phone, PrimarySubjectID, JoinDate)
                                    VALUES (@FullName, @Email, @Phone, @PrimarySubjectID, @JoinDate)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FullName", Teacher.FullName);
                        command.Parameters.AddWithValue("@Email", Teacher.Email);
                        command.Parameters.AddWithValue("@Phone", Teacher.Phone ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PrimarySubjectID", Teacher.PrimarySubjectID ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@JoinDate", Teacher.JoinDate);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                SuccessMessage = "Teacher added successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error adding teacher: " + ex.Message;
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
                    string query = @"UPDATE Teachers SET 
                                    FullName = @FullName,
                                    Email = @Email,
                                    Phone = @Phone,
                                    PrimarySubjectID = @PrimarySubjectID,
                                    JoinDate = @JoinDate
                                    WHERE TeacherID = @TeacherID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TeacherID", teacherId);
                        command.Parameters.AddWithValue("@FullName", fullName);
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@Phone", phone ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PrimarySubjectID", primarySubjectID ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@JoinDate", joinDate);

                        int affectedRows = await command.ExecuteNonQueryAsync();
                        if (affectedRows == 0)
                        {
                            ErrorMessage = "Teacher not found";
                            return RedirectToPage();
                        }
                    }
                }

                SuccessMessage = "Teacher updated successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating teacher: " + ex.Message;
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
                    string query = "DELETE FROM Teachers WHERE TeacherID = @TeacherID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TeacherID", teacherId);
                        int affectedRows = await command.ExecuteNonQueryAsync();

                        if (affectedRows == 0)
                        {
                            ErrorMessage = "Teacher not found";
                            return RedirectToPage();
                        }
                    }
                }

                SuccessMessage = "Teacher deleted successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error deleting teacher: " + ex.Message;
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