using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace School_Management_System.Pages
{
    public class SubjectManagementModel : PageModel
    {
        [BindProperty]
        public SubjectInputModel Subject { get; set; } = new SubjectInputModel();

        public List<SubjectInfo> Subjects { get; set; } = new List<SubjectInfo>();
        public List<SelectListItem> AvailableClasses { get; set; } = new List<SelectListItem>();
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        private readonly string _connectionString = "Data Source=DESKTOP-U1827CH\\SQLEXPRESS;Initial Catalog=SchoolSysDB;Integrated Security=True;TrustServerCertificate=True";

        public async Task OnGetAsync()
        {
            await LoadSubjectsAsync();
            await LoadAvailableClassesAsync();
        }

        public async Task<IActionResult> OnPostAddSubjectAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSubjectsAsync();
                await LoadAvailableClassesAsync();
                return Page();
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"INSERT INTO Subjects (SubjectName, SubjectCode, Classid)
                                VALUES (@SubjectName, @SubjectCode, @ClassID)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SubjectName", Subject.SubjectName);
                        command.Parameters.AddWithValue("@SubjectCode", Subject.SubjectCode);
                        command.Parameters.AddWithValue("@ClassID", Subject.ClassID);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                SuccessMessage = "Subject added successfully!";
                ModelState.Clear();
                await LoadSubjectsAsync();
                await LoadAvailableClassesAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
                await LoadSubjectsAsync();
                await LoadAvailableClassesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditSubjectAsync(int subjectId, string subjectName, string subjectCode, int classId)
        {
            if (!ModelState.IsValid)
            {
                await LoadSubjectsAsync();
                await LoadAvailableClassesAsync();
                return Page();
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"UPDATE Subjects 
                                SET SubjectName = @SubjectName, 
                                    SubjectCode = @SubjectCode, 
                                    Classid = @ClassID,
                                    ModifiedDate = GETDATE()
                                WHERE SubjectID = @SubjectID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SubjectID", subjectId);
                        command.Parameters.AddWithValue("@SubjectName", subjectName);
                        command.Parameters.AddWithValue("@SubjectCode", subjectCode);
                        command.Parameters.AddWithValue("@ClassID", classId);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                SuccessMessage = "Subject updated successfully!";
                await LoadSubjectsAsync();
                await LoadAvailableClassesAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating subject: {ex.Message}";
                await LoadSubjectsAsync();
                await LoadAvailableClassesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteSubjectAsync(int subjectId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "DELETE FROM Subjects WHERE Subjectid = @SubjectID";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SubjectID", subjectId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                SuccessMessage = "Subject deleted successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting subject: {ex.Message}";
            }

            await LoadSubjectsAsync();
            await LoadAvailableClassesAsync();
            return Page();
        }

        private async Task LoadSubjectsAsync()
        {
            Subjects.Clear();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"SELECT s.SubjectID, s.SubjectName, s.SubjectCode, 
                                s.Classid, c.ClassName + ' - ' + c.Section AS ClassDisplayName
                                FROM Subjects s
                                JOIN Classes c ON s.Classid = c.ClassID
                                ORDER BY s.SubjectName";

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Subjects.Add(new SubjectInfo
                                {
                                    SubjectID = reader.GetInt32(0),
                                    SubjectName = reader.GetString(1),
                                    SubjectCode = reader.GetString(2),
                                    ClassID = reader.GetInt32(3),
                                    ClassName = reader.GetString(4)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading subjects: {ex.Message}";
            }
        }

        private async Task LoadAvailableClassesAsync()
        {
            AvailableClasses.Clear();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT ClassID, ClassName + ' - ' + Section AS ClassDisplayName FROM Classes ORDER BY ClassName, Section";
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                AvailableClasses.Add(new SelectListItem
                                {
                                    Value = reader.GetInt32(0).ToString(),
                                    Text = reader.GetString(1)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading classes: {ex.Message}";
            }
        }
    }

    public class SubjectInputModel
    {
        [Required(ErrorMessage = "Subject name is required")]
        [StringLength(100, ErrorMessage = "Subject name cannot be longer than 100 characters")]
        public string SubjectName { get; set; }

        [Required(ErrorMessage = "Subject code is required")]
        [StringLength(20, ErrorMessage = "Subject code cannot be longer than 20 characters")]
        public string SubjectCode { get; set; }

        [Required(ErrorMessage = "Class is required")]
        public int ClassID { get; set; }
    }

    public class SubjectInfo
    {
        public int SubjectID { get; set; }
        public string SubjectName { get; set; }
        public string SubjectCode { get; set; }
        public int ClassID { get; set; }
        public string ClassName { get; set; }
    }
}
