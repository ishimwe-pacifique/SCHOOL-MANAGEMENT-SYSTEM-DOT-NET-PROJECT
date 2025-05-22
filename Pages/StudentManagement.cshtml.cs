using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace School_Management_System.Pages
{
    public class StudentManagementModel : PageModel
    {
        private readonly string connectionString = "Data Source=DESKTOP-14GLE6P\\SQLEXPRESS;Initial Catalog=SchoolSysDB;Integrated Security=True;TrustServerCertificate=True";

        [BindProperty]
        public Student Student { get; set; }

        public List<StudentDisplay> Students { get; set; } = new List<StudentDisplay>();
        public List<SelectListItem> Classes { get; set; } = new List<SelectListItem>();

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadStudents();
            await LoadClasses();
        }

        private async Task LoadStudents()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"SELECT s.StudentID, s.FullName, s.Email, s.DateOfBirth, 
                                s.ClassID, c.ClassName + ' - ' + c.Section AS ClassInfo, 
                                s.ParentGuardianName, s.ParentGuardianPhone, 
                                s.EnrollmentDate, s.IsActive 
                                FROM Students s
                                LEFT JOIN Classes c ON s.ClassID = c.ClassID";

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Students.Add(new StudentDisplay
                        {
                            StudentID = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(2),
                            DateOfBirth = reader.GetDateTime(3),
                            ClassID = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            ClassName = reader.IsDBNull(5) ? "N/A" : reader.GetString(5),
                            ParentGuardianName = reader.GetString(6),
                            ParentGuardianPhone = reader.GetString(7),
                            EnrollmentDate = reader.GetDateTime(8),
                            IsActive = reader.GetBoolean(9)
                        });
                    }
                }
            }
        }

        private async Task LoadClasses()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT ClassID, ClassName + ' - ' + Section AS ClassInfo FROM Classes", connection);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    Classes.Clear();
                    while (await reader.ReadAsync())
                    {
                        Classes.Add(new SelectListItem
                        {
                            Value = reader.GetInt32(0).ToString(),
                            Text = reader.GetString(1)
                        });
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostAddStudentAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadStudents();
                await LoadClasses();
                return Page();
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var checkEmailQuery = "SELECT COUNT(*) FROM Students WHERE Email = @Email";
                    using (var checkCommand = new SqlCommand(checkEmailQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", Student.Email);
                        var exists = (int)await checkCommand.ExecuteScalarAsync();
                        if (exists > 0)
                        {
                            ErrorMessage = "A student with this email already exists.";
                            return RedirectToPage();
                        }
                    }

                    string query = @"INSERT INTO Students 
                                    (FullName, Email, DateOfBirth, ClassID, 
                                    ParentGuardianName, ParentGuardianPhone, 
                                    EnrollmentDate, IsActive)
                                    VALUES (@FullName, @Email, @DateOfBirth, @ClassID,
                                    @ParentGuardianName, @ParentGuardianPhone,
                                    @EnrollmentDate, @IsActive)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FullName", Student.FullName);
                        command.Parameters.AddWithValue("@Email", Student.Email);
                        command.Parameters.AddWithValue("@DateOfBirth", Student.DateOfBirth);
                        command.Parameters.AddWithValue("@ClassID", Student.ClassID ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ParentGuardianName", Student.ParentGuardianName);
                        command.Parameters.AddWithValue("@ParentGuardianPhone", Student.ParentGuardianPhone);
                        command.Parameters.AddWithValue("@EnrollmentDate", Student.EnrollmentDate);
                        command.Parameters.AddWithValue("@IsActive", Student.IsActive);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                SuccessMessage = "Student added successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error adding student: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostEditStudentAsync(int studentId, string fullName, string email,
            DateTime dateOfBirth, int? classID, string parentGuardianName,
            string parentGuardianPhone, DateTime enrollmentDate, bool isActive)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"UPDATE Students SET 
                                    FullName = @FullName,
                                    Email = @Email,
                                    DateOfBirth = @DateOfBirth,
                                    ClassID = @ClassID,
                                    ParentGuardianName = @ParentGuardianName,
                                    ParentGuardianPhone = @ParentGuardianPhone,
                                    EnrollmentDate = @EnrollmentDate,
                                    IsActive = @IsActive
                                    WHERE StudentID = @StudentID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StudentID", studentId);
                        command.Parameters.AddWithValue("@FullName", fullName);
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                        command.Parameters.AddWithValue("@ClassID", classID ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ParentGuardianName", parentGuardianName);
                        command.Parameters.AddWithValue("@ParentGuardianPhone", parentGuardianPhone);
                        command.Parameters.AddWithValue("@EnrollmentDate", enrollmentDate);
                        command.Parameters.AddWithValue("@IsActive", isActive);

                        int affectedRows = await command.ExecuteNonQueryAsync();
                        if (affectedRows == 0)
                        {
                            ErrorMessage = "Student not found";
                            return RedirectToPage();
                        }
                    }
                }

                SuccessMessage = "Student updated successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating student: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteStudent(int studentId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM Students WHERE StudentID = @StudentID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StudentID", studentId);
                        int affectedRows = await command.ExecuteNonQueryAsync();

                        if (affectedRows == 0)
                        {
                            ErrorMessage = "Student not found";
                            return RedirectToPage();
                        }
                    }
                }

                SuccessMessage = "Student deleted successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error deleting student: " + ex.Message;
                return RedirectToPage();
            }
        }
    }

    public class Student
    {
        public int StudentID { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Class")]
        public int? ClassID { get; set; }

        [Required(ErrorMessage = "Parent/Guardian name is required")]
        [StringLength(100)]
        public string ParentGuardianName { get; set; }

        [Required(ErrorMessage = "Parent/Guardian phone is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20)]
        public string ParentGuardianPhone { get; set; }

        [Required(ErrorMessage = "Enrollment date is required")]
        [DataType(DataType.Date)]
        public DateTime EnrollmentDate { get; set; }

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;
    }

    public class StudentDisplay
    {
        public int StudentID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int? ClassID { get; set; }
        public string ClassName { get; set; }
        public string ParentGuardianName { get; set; }
        public string ParentGuardianPhone { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public bool IsActive { get; set; }
    }
}