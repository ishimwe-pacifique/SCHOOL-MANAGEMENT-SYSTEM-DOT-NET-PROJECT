using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuestPDF.Helpers;
using System.Data.SqlClient;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using QuestPDF.Previewer;

namespace School_Management_System.Pages
{
    public static class DBConnect
    {
        public static SqlConnection GetConnection()
        {
            return new SqlConnection("Data Source=DESKTOP-14GLE6P\\SQLEXPRESS;Initial Catalog=SchoolSysDB;Integrated Security=True;TrustServerCertificate=True");
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

                // Get class name and section
                using (var cmd = new SqlCommand("SELECT ClassName, Section FROM Classes WHERE ClassId = @ClassId", conn))
                {
                    cmd.Parameters.AddWithValue("@ClassId", ClassId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ClassName = $"{reader.GetString(0)} - {reader.GetString(1)}";
                        }
                    }
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
                    var result = cmd.ExecuteScalar();
                    totalDays = result != DBNull.Value ? (int)result : 0;
                }

                foreach (var student in studentList)
                {
                    int present = 0;
                    int absent = 0;

                    // Get present days
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM Attendance WHERE Student = @StudentId AND Status = 'Present' AND Class = @ClassId", conn))
                    {
                        cmd.Parameters.AddWithValue("@StudentId", student.StudentId);
                        cmd.Parameters.AddWithValue("@ClassId", ClassId);
                        present = (int)cmd.ExecuteScalar();
                    }

                    // Get absent days
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM Attendance WHERE Student = @StudentId AND Status = 'Absent' AND Class = @ClassId", conn))
                    {
                        cmd.Parameters.AddWithValue("@StudentId", student.StudentId);
                        cmd.Parameters.AddWithValue("@ClassId", ClassId);
                        absent = (int)cmd.ExecuteScalar();
                    }

                    // Calculate total attended days (present + absent)
                    int totalAttendedDays = present + absent;

                    // Calculate percentage
                    double percentage = 0;
                    if (totalAttendedDays > 0)
                    {
                        // Calculate based on actual attendance records (present/totalAttendedDays)
                        // But scale it to the total possible days (totalDays)
                        percentage = Math.Round((present * 100.0) / totalAttendedDays, 2);

                        // Ensure percentage is between 0-100%
                        percentage = Math.Max(0, Math.Min(percentage, 100));
                    }

                    AttendanceReport.Add(new AttendanceSummary
                    {
                        StudentName = student.FullName,
                        DaysPresent = present,
                        DaysAbsent = absent,
                        AttendancePercentage = percentage
                    });
                }

                conn.Close();
            }
        }



        public IActionResult OnPostExportExcel()
        {
            OnPost(); // This will populate AttendanceReport based on ClassId

            // Example: Generate a CSV as Excel content
            var csv = new StringBuilder();
            csv.AppendLine("Student Name,Days Present,Days Absent,Attendance %");
            foreach (var item in AttendanceReport)
            {
                csv.AppendLine($"{item.StudentName},{item.DaysPresent},{item.DaysAbsent},{item.AttendancePercentage}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"AttendanceReport_Class_{ClassId}.csv");
        }



        public IActionResult OnPostExportPdf()
        {
            OnPost(); // This will populate AttendanceReport based on ClassId

            var pdfDoc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text($"Attendance Report - {ClassName}").Bold().FontSize(18);
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(180);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(100);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Student Name");
                            header.Cell().Element(CellStyle).Text("Present");
                            header.Cell().Element(CellStyle).Text("Absent");
                            header.Cell().Element(CellStyle).Text("Attendance %");
                        });

                        foreach (var record in AttendanceReport)
                        {
                            table.Cell().Element(CellStyle).Text(record.StudentName);
                            table.Cell().Element(CellStyle).Text(record.DaysPresent.ToString());
                            table.Cell().Element(CellStyle).Text(record.DaysAbsent.ToString());
                            table.Cell().Element(CellStyle).Text($"{record.AttendancePercentage}%");
                        }
                    });
                });
            });

            byte[] pdfBytes = pdfDoc.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"AttendanceReport_Class_{ClassId}.pdf");

            static IContainer CellStyle(IContainer container) =>
                container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
        }



        private void LoadClassList()
        {
            ClassList = new List<SelectListItem>();

            using (var conn = DBConnect.GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT ClassId, ClassName, Section FROM Classes", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ClassList.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = $"{reader.GetString(1)} - {reader.GetString(2)}"
                            });
                        }
                    }
                }
                conn.Close();
            }
        }
    }
}