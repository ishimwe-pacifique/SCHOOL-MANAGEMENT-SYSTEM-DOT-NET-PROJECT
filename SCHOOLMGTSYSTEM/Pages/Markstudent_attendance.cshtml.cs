using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SCHOOLMGTSYSTEM.Pages
{
    public class MarkStudentAttendanceModel : PageModel
    {
        // Properties for class selection and date
        [BindProperty]
        public int SelectedClassId { get; set; }

        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; } = DateTime.Today;

        // Lists for dropdowns and data display
        public List<ClassInfo> Classes { get; set; }
        public List<StudentInfo> Students { get; set; }

        [BindProperty]
        public List<AttendanceRecord> Attendance { get; set; }

        // Flag to control UI display
        public bool StudentsLoaded { get; set; }

        // Success message
        [TempData]
        public string SuccessMessage { get; set; }

        public void OnGet()
        {
            // Initialize properties 
            Classes = GetClasses();
            Students = new List<StudentInfo>();
            Attendance = new List<AttendanceRecord>();
            StudentsLoaded = false;

            // Set default date to today
            AttendanceDate = DateTime.Today;
        }

        public IActionResult OnPostLoadStudents()
        {
            // Validation
            if (SelectedClassId <= 0)
            {
                ModelState.AddModelError("SelectedClassId", "Please select a class.");
                Classes = GetClasses();
                return Page();
            }

            // Load classes for dropdown
            Classes = GetClasses();

            // Load students for the selected class
            Students = GetStudentsByClass(SelectedClassId);

            // Initialize attendance records
            Attendance = new List<AttendanceRecord>();
            foreach (var student in Students)
            {
                Attendance.Add(new AttendanceRecord
                {
                    StudentId = student.Id,
                    Status = "Present" // Default status
                });
            }

            StudentsLoaded = true;
            return Page();
        }

        public IActionResult OnPostSaveAttendance()
        {
            // Validation
            if (SelectedClassId <= 0)
            {
                ModelState.AddModelError("SelectedClassId", "Please select a class.");
                Classes = GetClasses();
                return Page();
            }

            // Load classes and students again for the page
            Classes = GetClasses();
            Students = GetStudentsByClass(SelectedClassId);
            StudentsLoaded = true;

            // Save attendance records to database
            SaveAttendanceRecords(SelectedClassId, AttendanceDate, Attendance);

            // Set success message
            SuccessMessage = "Attendance has been successfully saved!";

            return RedirectToPage();
        }

        // Helper methods to get data (in a real app, these would interact with a database)
        private List<ClassInfo> GetClasses()
        {
            // In a real application, this would fetch data from a database
            return new List<ClassInfo>
            {
                new ClassInfo { Id = 1, Name = "Class 1A" },
                new ClassInfo { Id = 2, Name = "Class 2B" },
                new ClassInfo { Id = 3, Name = "Class 3C" },
                new ClassInfo { Id = 4, Name = "Class 4D" },
                new ClassInfo { Id = 5, Name = "Class 5E" }
            };
        }

        private List<StudentInfo> GetStudentsByClass(int classId)
        {
            // In a real application, this would fetch data from a database based on classId
            var students = new List<StudentInfo>();

            // Sample data
            if (classId == 1)
            {
                students.Add(new StudentInfo { Id = 1, RollNumber = "001", FullName = "John Smith" });
                students.Add(new StudentInfo { Id = 2, RollNumber = "002", FullName = "Jane Doe" });
                students.Add(new StudentInfo { Id = 3, RollNumber = "003", FullName = "Michael Johnson" });
            }
            else if (classId == 2)
            {
                students.Add(new StudentInfo { Id = 4, RollNumber = "004", FullName = "Emily Brown" });
                students.Add(new StudentInfo { Id = 5, RollNumber = "005", FullName = "David Wilson" });
            }
            // Add more sample data for other classes if needed

            return students;
        }

        private void SaveAttendanceRecords(int classId, DateTime date, List<AttendanceRecord> attendance)
        {
            // In a real application, this would save data to a database
            // For now, we'll just log the data (you would replace this with actual database operations)
            Console.WriteLine($"Saving attendance for Class ID: {classId}, Date: {date.ToShortDateString()}");
            foreach (var record in attendance)
            {
                Console.WriteLine($"Student ID: {record.StudentId}, Status: {record.Status}");
            }
        }
    }

    // Model classes
    public class ClassInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class StudentInfo
    {
        public int Id { get; set; }
        public string RollNumber { get; set; }
        public string FullName { get; set; }
    }

    public class AttendanceRecord
    {
        public int StudentId { get; set; }
        public string Status { get; set; } // "Present" or "Absent"
    }
}