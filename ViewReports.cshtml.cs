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

namespace School_Management_System.Pages
{
    public class ViewReportsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ViewReportsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public int TotalSubjects { get; set; }
        public double SuccessRate { get; set; }
        public List<HighScoringStudent> TopStudents { get; set; } = new();

        public void OnGet()
        {
            string connectionString = _configuration.GetConnectionString("SchoolConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
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
                    SuccessRate = result != DBNull.Value ? Convert.ToDouble(result) : 0;
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
                            AverageScore = reader.GetDouble(1)
                        });
                    }
                }
            }
        }

        public IActionResult OnGetExportExcel()
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Report");

            sheet.Cells[1, 1].Value = "Metric";
            sheet.Cells[1, 2].Value = "Value";
            sheet.Cells[2, 1].Value = "Total Students";
            sheet.Cells[2, 2].Value = TotalStudents;
            sheet.Cells[3, 1].Value = "Total Classes";
            sheet.Cells[3, 2].Value = TotalClasses;
            sheet.Cells[4, 1].Value = "Total Subjects";
            sheet.Cells[4, 2].Value = TotalSubjects;
            sheet.Cells[5, 1].Value = "Success Rate";
            sheet.Cells[5, 2].Value = $"{SuccessRate}%";

            int row = 7;
            sheet.Cells[row, 1].Value = "Top Scoring Students";
            sheet.Cells[row, 2].Value = "Avg Score";

            foreach (var student in TopStudents)
            {
                row++;
                sheet.Cells[row, 1].Value = student.Name;
                sheet.Cells[row, 2].Value = student.AverageScore;
            }

            byte[] bytes = package.GetAsByteArray();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Report.xlsx");
        }

        public IActionResult OnGetExportPdf()
        {
            var doc = new PdfDocument();
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // ✅ Use PdfSharpCore.Drawing.XFontStyle
            var font = new XFont("Arial", 14, XFontStyle.Bold);
            var textFont = new XFont("Arial", 12, XFontStyle.Regular);

            gfx.DrawString("School Performance Report", font, XBrushes.Black, new XRect(0, 20, page.Width, 0), XStringFormats.TopCenter);
            gfx.DrawString($"Total Students: {TotalStudents}", textFont, XBrushes.Black, 40, 60);
            gfx.DrawString($"Total Classes: {TotalClasses}", textFont, XBrushes.Black, 40, 80);
            gfx.DrawString($"Total Subjects: {TotalSubjects}", textFont, XBrushes.Black, 40, 100);
            gfx.DrawString($"Success Rate: {SuccessRate}%", textFont, XBrushes.Black, 40, 120);

            int y = 160;
            gfx.DrawString("Top Scoring Students:", font, XBrushes.Black, 40, y);
            y += 20;

            foreach (var student in TopStudents)
            {
                gfx.DrawString($"{student.Name} - {student.AverageScore}", textFont, XBrushes.Black, 40, y);
                y += 20;
            }

            using var stream = new MemoryStream();
            doc.Save(stream, false);
            return File(stream.ToArray(), "application/pdf", "Report.pdf");
        }

        public class HighScoringStudent
        {
            public string Name { get; set; }
            public double AverageScore { get; set; }
        }
    }

    // Extra enum for font style if using PdfSharpCore
    public enum XFontStyleEx
    {
        Regular = 0,
        Bold = 1
    }
}
