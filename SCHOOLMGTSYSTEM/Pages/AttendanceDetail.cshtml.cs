using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SCHOOLMGTSYSTEM.Pages
{
    public class AttendanceDetailModel : PageModel
    {
        // Properties for filtering
        [BindProperty]
        public int? SelectedClassId { get; set; }

        [BindProperty]
        public int? SelectedStudentId { get; set; }

        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        // Lists for dropdowns and data display
        public List<ClassInfo> Classes { get; set; }
        public List<StudentInfo> Students { get; set; }
        public List<AttendanceRecordDetail> AttendanceRecords { get; set; }

        // Summary statistics
        public int PresentRate { get; set; }
        public int LateRate { get; set; }
        public int AbsentRate { get; set; }
        public int OverallAttendance { get; set; }

        public void OnGet()
        {
            // Initialize properties with default values
            InitializeProperties();

            // Load initial data
            LoadData();
        }

        public IActionResult OnPost()
        {
            // This will handle the form submission when changing the class
            // to load students for the selected class
            InitializeProperties();

            if (SelectedClassId.HasValue && SelectedClassId.Value > 0)
            {
                Students = GetStudentsByClass(SelectedClassId.Value);
            }

            LoadData();
            return Page();
        }

        public IActionResult OnPostFilter()
        {
            // This will handle the filter button click
            InitializeProperties();

            if (SelectedClassId.HasValue && SelectedClassId.Value > 0)
            {
                Students = GetStudentsByClass(SelectedClassId.Value);
            }

            // Apply filters to attendance records
            LoadData(true);

            // Calculate summary statistics based on filtered data
            CalculateStatistics();

            return Page();
        }

        private void InitializeProperties()
        {
            // Initialize collections
            Classes = GetClasses();
            Students = new List<StudentInfo>();
            AttendanceRecords = new List<AttendanceRecordDetail>();

            // Set default statistics
            PresentRate = 0;
            LateRate = 0;
            AbsentRate = 0;
            OverallAttendance = 0;

            // Set default dates if not provided
            if (!FromDate.HasValue)
            {
                FromDate = DateTime.Today.AddDays(-30); // Default to last 30 days
            }

            if (!ToDate.HasValue)
            {
                ToDate = DateTime.Today;
            }
        }

        private void LoadData(bool applyFilters = false)
        {
            if (!applyFilters)
            {
                // Just load empty data or initial data
                AttendanceRecords = new List<AttendanceRecordDetail>();
                return;
            }

            // Get attendance records based on filters
            AttendanceRecords = GetAttendanceRecords(
                SelectedClassId,
                SelectedStudentId,
                FromDate,
                ToDate);
        }

        private void CalculateStatistics()
        {
            if (AttendanceRecords.Count == 0)
            {
                return; // No records to calculate stats for
            }

            int totalRecords = AttendanceRecords.Count;
            int presentCount = AttendanceRecords.Count(r => r.Status == "Present");
            int lateCount = AttendanceRecords.Count(r => r.Status == "Late");
            int absentCount = AttendanceRecords.Count(r => r.Status == "Absent");

            PresentRate = (int)Math.Round((double)presentCount / totalRecords * 100);
            LateRate = (int)Math.Round((double)lateCount / totalRecords * 100);
            AbsentRate = (int)Math.Round((double)absentCount / totalRecords * 100);
            OverallAttendance = PresentRate + LateRate; // Present + Late
        }

        // Helper methods for data retrieval
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

        private List<AttendanceRecordDetail> GetAttendanceRecords(
            int? classId,
            int? studentId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            // In a real application, this would fetch data from a database based on the filters
            var records = new List<AttendanceRecordDetail>();

            // If no filters are applied, return empty list
            if (!classId.HasValue && !studentId.HasValue)
            {
                return records;
            }

            // Sample data - this would come from your database in a real application
            if (classId == 1)
            {
                // Generate some sample attendance records
                var startDate = fromDate ?? DateTime.Today.AddDays(-30);
                var endDate = toDate ?? DateTime.Today;

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    // Skip weekends
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        continue;

                    // Only include records for the selected student if one is selected
                    if (studentId.HasValue)
                    {
                        var student = GetStudentsByClass(classId.Value)
                            .FirstOrDefault(s => s.Id == studentId.Value);

                        if (student != null)
                        {
                            var status = GetRandomStatus();
                            records.Add(new AttendanceRecordDetail
                            {
                                Date = date,
                                StudentId = student.Id,
                                StudentName = student.FullName,
                                ClassId = classId.Value,
                                ClassName = "Class 1A",
                                Status = status
                            });
                        }
                    }
                    else
                    {
                        // Include records for all students in the class
                        foreach (var student in GetStudentsByClass(classId.Value))
                        {
                            var status = GetRandomStatus();
                            records.Add(new AttendanceRecordDetail
                            {
                                Date = date,
                                StudentId = student.Id,
                                StudentName = student.FullName,
                                ClassId = classId.Value,
                                ClassName = "Class 1A",
                                Status = status
                            });
                        }
                    }
                }
            }

            return records;
        }

        private string GetRandomStatus()
        {
            // Helper to generate random attendance status for demo purposes
            var random = new Random();
            int value = random.Next(0, 100);

            if (value < 80)
                return "Present";
            else if (value < 90)
                return "Late";
            else
                return "Absent";
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

    public class AttendanceRecordDetail
    {
        public DateTime Date { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string Status { get; set; } // "Present", "Late", or "Absent"
    }
}