using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
namespace School_Management_System.Pages
{
    public class AttendanceModel : PageModel
    {
        private readonly string _connectionString = "Data Source=DESKTOP-BUVN895;Initial Catalog=SchoolSysDB-2;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";

        [BindProperty]
        public int ClassId { get; set; }

        public string ClassName { get; set; }

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

            ClassName = GetClassName(ClassId);
            return Page();
        }

        public IActionResult OnPostSaveAttendance(List<StudentViewModel> Students)
        {
            Console.WriteLine("⚠️ OnPostSaveAttendance called");
            Console.WriteLine("ClassId: " + ClassId);
            Console.WriteLine("Students count: " + (Students?.Count ?? 0));

            LoadClasses();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    foreach (var student in Students)
                    {
                        Console.WriteLine($"Saving: {student.FullName}, Present: {student.IsPresent}");

                        var cmd = new SqlCommand("INSERT INTO Attendance (Class, Student, AttendanceDate, Status) VALUES (@ClassId, @StudentId, @Date, @Status)", connection);
                        cmd.Parameters.AddWithValue("@ClassId", ClassId);
                        cmd.Parameters.AddWithValue("@StudentId", student.StudentId);
                        cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                        cmd.Parameters.AddWithValue("@Status", student.IsPresent ? "Present" : "Absent");
                        cmd.ExecuteNonQuery();
                    }
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


        private void LoadClasses()
        {
            ClassList = new List<SelectListItem>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand("SELECT ClassId, ClassName FROM Classes", connection);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ClassList.Add(new SelectListItem
                    {
                        Value = reader["ClassId"].ToString(),
                        Text = reader["ClassName"].ToString()
                    });
                }
            }
        }




        public class PDFExportHelper
        {
            public static byte[] ExportToPdf(List<StudentViewModel> students, string className)
            {
                using (var ms = new MemoryStream())
                {
                    var document = new PdfDocument();
                    var page = document.AddPage();
                    var gfx = XGraphics.FromPdfPage(page);
                    var font = new XFont("Arial", 12);

                    gfx.DrawString($"Attendance for {className}", font, XBrushes.Black, 20, 40);

                    int yPosition = 70;
                    foreach (var student in students)
                    {
                        gfx.DrawString($"{student.FullName} - {(student.IsPresent ? "Present" : "Absent")}", font, XBrushes.Black, 20, yPosition);
                        yPosition += 20;
                    }

                    document.Save(ms);
                    return ms.ToArray();
                }
            }
        }





        public IActionResult OnGetExportExcel()
        {
            var excelData = ExportHelper.ExportToExcel(Students, ClassName);
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "attendance.xlsx");
        }

        public IActionResult OnGetExportPdf()
        {
            var pdfData = PDFExportHelper.ExportToPdf(Students, ClassName);
            return File(pdfData, "application/pdf", "attendance.pdf");
        }





        public class ExportHelper
        {
            public static byte[] ExportToExcel(List<StudentViewModel> students, string className)
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Attendance");
                    worksheet.Cells[1, 1].Value = "Student Name";
                    worksheet.Cells[1, 2].Value = "Present";

                    int row = 2;
                    foreach (var student in students)
                    {
                        worksheet.Cells[row, 1].Value = student.FullName;
                        worksheet.Cells[row, 2].Value = student.IsPresent ? "Present" : "Absent";
                        row++;
                    }

                    var excelData = package.GetAsByteArray();
                    return excelData;
                }
            }
        }



        private string GetClassName(int classId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SqlCommand("SELECT ClassName FROM Classes WHERE ClassId = @ClassId", connection);
                cmd.Parameters.AddWithValue("@ClassId", classId);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "";
            }
        }
    }

    // New simplified ViewModel for students (does NOT affect database)
    public class StudentViewModel
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public bool IsPresent { get; set; }
    }
}
