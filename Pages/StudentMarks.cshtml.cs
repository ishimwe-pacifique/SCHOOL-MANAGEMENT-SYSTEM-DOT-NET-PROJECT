using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

public class StudentMarksModel : PageModel
{
    private readonly string connectionString = "Data Source=DESKTOP-14GLE6P\\SQLEXPRESS;Initial Catalog=SchoolSysDB;Integrated Security=True;TrustServerCertificate=True";



    public List<SelectListItem> ClassOptions { get; set; } = new();
    public List<SelectListItem> SubjectOptions { get; set; } = new();
    public List<SelectListItem> StudentOptions { get; set; } = new();

    [BindProperty]
    public int? SelectedClassId { get; set; }

    [BindProperty]
    public int? SelectedStudentId { get; set; }

    [BindProperty]
    public string SelectedSubject { get; set; }

    [BindProperty]
    public int? Mark { get; set; }

    [BindProperty]
    public DateTime? DateOfExam { get; set; }

    public string SuccessMessage { get; set; }

    public List<StudentMark> StudentMarks { get; set; } = new();

    public void OnGet()
    {
        LoadDropdowns();
    }

    public IActionResult OnPost()
    {
        LoadDropdowns();

        if (SelectedClassId.HasValue && SelectedStudentId.HasValue &&
            !string.IsNullOrEmpty(SelectedSubject) && Mark.HasValue && DateOfExam.HasValue)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string insertQuery = "INSERT INTO StudentMarks (StudentId, Subject, Mark, DateOfExam) VALUES (@StudentId, @Subject, @Mark, @DateOfExam)";
                SqlCommand cmd = new SqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("@StudentId", SelectedStudentId.Value);
                cmd.Parameters.AddWithValue("@Subject", SelectedSubject);
                cmd.Parameters.AddWithValue("@Mark", Mark.Value);
                cmd.Parameters.AddWithValue("@DateOfExam", DateOfExam.Value);
                cmd.ExecuteNonQuery();
            }

            SuccessMessage = "Student mark saved successfully!";
        }

        if (SelectedClassId.HasValue)
        {
            LoadStudentMarks(SelectedClassId.Value);
        }

        return Page();
    }

    private void LoadDropdowns()
    {
        ClassOptions = new List<SelectListItem>();
        SubjectOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Math", Text = "Math" },
                new SelectListItem { Value = "Science", Text = "Science" },
                new SelectListItem { Value = "English", Text = "English" }
            };
        StudentOptions = new List<SelectListItem>();

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            // Load classes with sections
            var classCmd = new SqlCommand("SELECT ClassId, ClassName + ' - ' + Section AS ClassInfo FROM Classes", conn);
            var classReader = classCmd.ExecuteReader();
            while (classReader.Read())
            {
                ClassOptions.Add(new SelectListItem
                {
                    Value = classReader["ClassId"].ToString(),
                    Text = classReader["ClassInfo"].ToString()
                });
            }
            classReader.Close();

            // Load students for selected class
            if (SelectedClassId.HasValue)
            {
                var studentCmd = new SqlCommand("SELECT StudentId, FullName FROM Students WHERE ClassID = @ClassId", conn);
                studentCmd.Parameters.AddWithValue("@ClassId", SelectedClassId.Value);
                var studentReader = studentCmd.ExecuteReader();
                while (studentReader.Read())
                {
                    StudentOptions.Add(new SelectListItem
                    {
                        Value = studentReader["StudentId"].ToString(),
                        Text = studentReader["FullName"].ToString()
                    });
                }
                studentReader.Close();
            }
        }
    }

    private void LoadStudentMarks(int classId)
    {
        StudentMarks = new List<StudentMark>();
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string query = @"
                SELECT s.FullName AS StudentName, m.Subject, m.Mark, m.DateOfExam
                FROM StudentMarks m
                JOIN Students s ON m.StudentId = s.StudentId
                WHERE s.ClassID = @ClassId
                ORDER BY m.DateOfExam DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ClassId", classId);

            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                StudentMarks.Add(new StudentMark
                {
                    StudentName = reader["StudentName"].ToString(),
                    Subject = reader["Subject"].ToString(),
                    Mark = Convert.ToInt32(reader["Mark"]),
                    DateOfExam = Convert.ToDateTime(reader["DateOfExam"])
                });
            }
        }
    }

    public class StudentMark
    {
        public string StudentName { get; set; }
        public string Subject { get; set; }
        public int Mark { get; set; }
        public DateTime DateOfExam { get; set; }
    }
}