using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace School_Management_System.Pages
{
    public class AttendanceModel : PageModel
    {
        private readonly string _connectionString = "Data Source=DESKTOP-14GLE6P\\SQLEXPRESS;Initial Catalog=SchoolSysDB;Integrated Security=True;TrustServerCertificate=True";

        [BindProperty]
        public int ClassId { get; set; }

        [BindProperty]
        public List<StudentViewModel> Students { get; set; } = new List<StudentViewModel>();

        public List<SelectListItem> ClassList { get; set; }

        public void OnGet()
        {
            LoadClasses();
        }

        public IActionResult OnPostLoadStudents()
        {
            LoadClasses();

            if (ClassId == 0)
            {
                ModelState.AddModelError("", "Please select a class.");
                return Page();
            }

            Students = new List<StudentViewModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT StudentId, FullName FROM Students WHERE ClassId = @ClassId", connection);
                command.Parameters.AddWithValue("@ClassId", ClassId);
                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Students.Add(new StudentViewModel
                    {
                        StudentId = (int)reader["StudentId"],
                        FullName = reader["FullName"].ToString(),
                        IsPresent = false
                    });
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAttendanceAsync()
        {
            LoadClasses();

            if (ClassId == 0)
            {
                ModelState.AddModelError("", "Please select a class.");
                return Page();
            }

            if (Students == null || Students.Count == 0)
            {
                ModelState.AddModelError("", "No students found to save attendance.");
                return Page();
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    foreach (var student in Students)
                    {
                        var cmd = new SqlCommand("INSERT INTO Attendance (Class, Student, AttendanceDate, Status) VALUES (@ClassId, @StudentId, @Date, @Status)", connection);
                        cmd.Parameters.AddWithValue("@ClassId", ClassId);
                        cmd.Parameters.AddWithValue("@StudentId", student.StudentId);
                        cmd.Parameters.AddWithValue("@Date", DateTime.Today);
                        cmd.Parameters.AddWithValue("@Status", student.IsPresent ? "Present" : "Absent");
                        cmd.ExecuteNonQuery();
                    }
                }

                string teacherEmail = GetTeacherEmailForClass(ClassId);
                string classInfo = GetClassNameWithSection(ClassId);

                if (!string.IsNullOrEmpty(teacherEmail))
                {
                    await SendAttendanceEmailAsync(teacherEmail, classInfo, Students);
                }

                TempData["Success"] = "Attendance saved successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error saving attendance: {ex.Message}");
                return Page();
            }
        }

        private string GetTeacherEmailForClass(int classId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand("SELECT Email FROM Teachers WHERE TeacherId = (SELECT TeacherId FROM Classes WHERE ClassId = @ClassId)", connection);
                cmd.Parameters.AddWithValue("@ClassId", classId);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "";
            }
        }

        private void LoadClasses()
        {
            ClassList = new List<SelectListItem>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand("SELECT ClassId, ClassName + ' - ' + Section AS ClassInfo FROM Classes", connection);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ClassList.Add(new SelectListItem
                    {
                        Value = reader["ClassId"].ToString(),
                        Text = reader["ClassInfo"].ToString()
                    });
                }
            }
        }

        private async Task SendAttendanceEmailAsync(string teacherEmail, string classInfo, List<StudentViewModel> students)
        {
            try
            {
                var subject = $"Attendance Report for {classInfo} on {DateTime.Now:yyyy-MM-dd}";
                var sb = new StringBuilder();
                sb.AppendLine($"Dear Teacher,");
                sb.AppendLine();
                sb.AppendLine($"Here is the attendance for your class {classInfo} on {DateTime.Now:yyyy-MM-dd}:");
                sb.AppendLine();

                foreach (var student in students)
                {
                    sb.AppendLine($"{student.FullName}: {(student.IsPresent ? "Present" : "Absent")}");
                }

                sb.AppendLine();
                sb.AppendLine("Regards,");
                sb.AppendLine("School Management System");

                var fromAddress = new MailAddress("amjules7@gmail.com", "School Management System");
                var toAddress = new MailAddress(teacherEmail);

                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential("amjules7@gmail.com", "your-app-password");
                    smtp.EnableSsl = true;

                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = sb.ToString(),
                        IsBodyHtml = false
                    })
                    {
                        await smtp.SendMailAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
        }

        public string GetClassNameWithSection(int classId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand("SELECT ClassName + ' - ' + Section AS ClassInfo FROM Classes WHERE ClassId = @ClassId", connection);
                cmd.Parameters.AddWithValue("@ClassId", classId);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "";
            }
        }
    }

    public class StudentViewModel
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public bool IsPresent { get; set; }
    }
}