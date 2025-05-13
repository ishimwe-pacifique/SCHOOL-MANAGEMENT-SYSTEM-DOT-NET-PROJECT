using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;

namespace School_Management_System.Pages
{// DBConnect.cs
   

    public static class DBConnect
    {
        public static SqlConnection GetConnection()
        {
            return new SqlConnection("Data Source=DESKTOP-BUVN895;Initial Catalog=SchoolSysDB-2;Integrated Security=True;Encrypt=True;TrustServerCertificate=True");
        }
    }

    public class AttendanceReportModel : PageModel
    {
        public class AttendanceSummary
        {
            public string StudentName { get; set; }
            public int DaysPresent { get; set; }
            public int DaysAbsent { get; set; }
            public double AttendancePercentage { get; set; }
        }

        [BindProperty] public int? ClassId { get; set; }
        public string ClassName { get; set; }
        public List<SelectListItem> ClassList { get; set; }
        public List<AttendanceSummary> AttendanceReport { get; set; }
        public bool ReportGenerated { get; set; } = false;

        public void OnGet()
        {
            LoadClassList();
        }


        public void OnPost()
        {
            LoadClassList();
            ReportGenerated = true;

            if (ClassId == null)
                return;

            AttendanceReport = new List<AttendanceSummary>();
            var studentList = new List<(int StudentId, string FullName)>();
            int totalDays = 0;

            using (var conn = DBConnect.GetConnection())
            {
                conn.Open();

                // Get class name
                using (var cmd = new SqlCommand("SELECT ClassName FROM Classes WHERE ClassId = @ClassId", conn))
                {
                    cmd.Parameters.AddWithValue("@ClassId", ClassId);
                    ClassName = cmd.ExecuteScalar()?.ToString();
                }

                // Get students in class
                using (var cmd = new SqlCommand("SELECT StudentId, FullName FROM Students WHERE ClassId = @ClassId", conn))
                {
                    cmd.Parameters.AddWithValue("@ClassId", ClassId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            studentList.Add((reader.GetInt32(0), reader.GetString(1)));
                        }
                    }
                }

                // Get total unique attendance dates for this class
                using (var cmd = new SqlCommand("SELECT COUNT(DISTINCT AttendanceDate) FROM Attendance WHERE Class = @ClassId", conn))
                {
                    cmd.Parameters.AddWithValue("@ClassId", ClassId);
                    totalDays = (int)cmd.ExecuteScalar();
                }

                // Loop through each student
                foreach (var student in studentList)
                {
                    int present = 0;

                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Attendance WHERE Student = @StudentId AND Status = 'Present'", conn))
                    {
                        cmd.Parameters.AddWithValue("@StudentId", student.StudentId);
                        present = (int)cmd.ExecuteScalar();
                    }

                    int absent = totalDays - present;

                    AttendanceReport.Add(new AttendanceSummary
                    {
                        StudentName = student.FullName,
                        DaysPresent = present,
                        DaysAbsent = absent,
                        AttendancePercentage = totalDays > 0 ? Math.Round((present * 100.0) / totalDays, 2) : 0
                    });
                }

                conn.Close();
            }
        }

        private void LoadClassList()
        {
            ClassList = new List<SelectListItem>();

            using (var conn = DBConnect.GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT ClassId, ClassName FROM Classes", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ClassList.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = reader.GetString(1)
                            });
                        }
                    }
                }
                conn.Close();
            }
        }
    }
}
