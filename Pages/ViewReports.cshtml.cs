using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using OfficeOpenXml;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Reflection.Metadata;


namespace School_Management_System.Pages
{
    public class ViewReportsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public ViewReportsModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("SchoolConnection");
        }

        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public int TotalSubjects { get; set; }
        public double SuccessRate { get; set; }
        public List<HighScoringStudent> TopStudents { get; set; } = new();

        public void OnGet()
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Total Students
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Students", connection))
                    TotalStudents = (int)cmd.ExecuteScalar();

                // Total Classes
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Classes", connection))
                    TotalClasses = (int)cmd.ExecuteScalar();

                // Total Subjects
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Subjects", connection))
                    TotalSubjects = (int)cmd.ExecuteScalar();

                // Success Rate
                using (SqlCommand cmd = new SqlCommand("SELECT AVG(CAST(Mark AS FLOAT)) FROM StudentMarks", connection))
                {
                    object result = cmd.ExecuteScalar();
                    SuccessRate = result != DBNull.Value ? Math.Round(Convert.ToDouble(result), 2) : 0;
                }

                // Top Students
                string topQuery = @"
                    SELECT TOP 5 S.FullName, AVG(CAST(M.Mark AS FLOAT)) AS AvgScore
                    FROM StudentMarks M
                    INNER JOIN Students S ON M.StudentId = S.StudentID
                    GROUP BY S.FullName
                    ORDER BY AvgScore DESC";

                using (SqlCommand cmd = new SqlCommand(topQuery, connection))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TopStudents.Add(new HighScoringStudent
                        {
                            Name = reader.GetString(0),
                            AverageScore = Math.Round(reader.GetDouble(1), 2)
                        });
                    }
                }
            }
        }




        public IActionResult OnGetExportPdf(int studentId)
        {
            if (studentId <= 0)
                return BadRequest("Invalid student ID");

            // Use the existing connection string
            string connectionString = _connectionString;

            // Variables to hold student info
            string fullName = "";
            string studentEmail = "";
            int classId = 0;

            var marksList = new List<(string Subject, int Mark, DateTime DateOfExam)>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // 1. Fetch student info by ID
                using (var cmd = new SqlCommand("SELECT TOP 1 StudentID, FullName, Email, ClassID FROM Students WHERE StudentID = @StudentID", connection))
                {
                    cmd.Parameters.AddWithValue("@StudentID", studentId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fullName = reader["FullName"].ToString();
                            studentEmail = reader["Email"].ToString();
                            classId = Convert.ToInt32(reader["ClassID"]);
                        }
                        else
                        {
                            return NotFound("Student not found");
                        }
                    }
                }

                // 2. Fetch marks for that student
                using (var cmd = new SqlCommand("SELECT Subject, Mark, DateOfExam FROM StudentMarks WHERE StudentId = @StudentID ORDER BY DateOfExam", connection))
                {
                    cmd.Parameters.AddWithValue("@StudentID", studentId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string subject = reader["Subject"].ToString();
                            int mark = Convert.ToInt32(reader["Mark"]);
                            DateTime dateOfExam = Convert.ToDateTime(reader["DateOfExam"]);
                            marksList.Add((subject, mark, dateOfExam));
                        }
                    }
                }
            }

            // 3. Generate the PDF using PdfSharpCore
            PdfDocument document = new PdfDocument();
            document.Info.Title = $"Marks Report for {fullName}";

            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont titleFont = new XFont("Verdana", 18, XFontStyle.Bold);
            XFont headerFont = new XFont("Verdana", 14, XFontStyle.Bold);
            XFont font = new XFont("Verdana", 12, XFontStyle.Regular);

            int startY = 50;

            gfx.DrawString($"Student: {fullName}", titleFont, XBrushes.Black, new XPoint(40, startY));
            startY += 40;
            gfx.DrawString($"Email: {studentEmail}", font, XBrushes.Black, new XPoint(40, startY));
            startY += 30;
            gfx.DrawString($"Class ID: {classId}", font, XBrushes.Black, new XPoint(40, startY));
            startY += 40;

            gfx.DrawString("Subject", headerFont, XBrushes.Black, new XPoint(40, startY));
            gfx.DrawString("Mark", headerFont, XBrushes.Black, new XPoint(200, startY));
            gfx.DrawString("Date of Exam", headerFont, XBrushes.Black, new XPoint(300, startY));
            startY += 25;
            gfx.DrawLine(XPens.Black, 40, startY, 500, startY);
            startY += 10;

            foreach (var mark in marksList)
            {
                gfx.DrawString(mark.Subject, font, XBrushes.Black, new XPoint(40, startY));
                gfx.DrawString(mark.Mark.ToString(), font, XBrushes.Black, new XPoint(200, startY));
                gfx.DrawString(mark.DateOfExam.ToString("yyyy-MM-dd"), font, XBrushes.Black, new XPoint(300, startY));
                startY += 25;

                // Add new page if space is insufficient
                if (startY > page.Height - 50)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    startY = 50;
                }
            }

            using var stream = new MemoryStream();
            document.Save(stream, false);
            stream.Position = 0;

            return File(stream.ToArray(), "application/pdf", $"MarksReport_{fullName}.pdf");
        }


        public JsonResult OnGetStudentsByClass(int classId)
        {
            var students = new List<Student>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand("SELECT StudentID, FullName FROM Students WHERE ClassID = @ClassID ORDER BY FullName", conn);
            cmd.Parameters.AddWithValue("@ClassID", classId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                students.Add(new Student
                {
                    StudentId = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }

            return new JsonResult(students);
        }

        public JsonResult OnGetTranscript(int studentId)
        {
            var transcript = new List<object>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
        SELECT M.Subject, M.Mark
        FROM StudentMarks M
        WHERE M.StudentId = @StudentID
        ORDER BY M.Subject", conn);

            cmd.Parameters.AddWithValue("@StudentID", studentId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                transcript.Add(new
                {
                    Subject = reader.GetString(0), // This is already the subject name in StudentMarks.Subject
                    Mark = reader.GetInt32(1)
                });
            }

            return new JsonResult(transcript);
        }


        public class HighScoringStudent
        {
            public string Name { get; set; }
            public double AverageScore { get; set; }
        }

        public class Student
        {
            public int StudentId { get; set; }
            public string Name { get; set; }
        }
    }
}