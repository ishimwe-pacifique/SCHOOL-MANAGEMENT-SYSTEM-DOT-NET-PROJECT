using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace School_Management_System.Pages
{
    public class ManageClassModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Class name is required")]
        public string ClassName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Section is required")]
        public string Section { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, 100, ErrorMessage = "Capacity must be between 1 and 100")]
        public int Capacity { get; set; }

        public List<ClassInfo> ClassList { get; set; } = new List<ClassInfo>();
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        private readonly string _connectionString = "Data Source=DESKTOP-U1827CH\\SQLEXPRESS;Initial Catalog=SchoolSysDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";

        public async Task OnGetAsync()
        {
            await LoadClassesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadClassesAsync();
                return Page();
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"INSERT INTO Classes (ClassName, Section, Capacity)
                                VALUES (@ClassName, @Section, @Capacity)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClassName", ClassName);
                        command.Parameters.AddWithValue("@Section", Section);
                        command.Parameters.AddWithValue("@Capacity", Capacity);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                SuccessMessage = "Class added successfully!";
                ModelState.Clear();
                await LoadClassesAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
                await LoadClassesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditAsync(int classId, string className, string section, int capacity)
        {
            if (!ModelState.IsValid)
            {
                await LoadClassesAsync();
                return Page();
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"UPDATE Classes 
                                SET ClassName = @ClassName, 
                                    Section = @Section, 
                                    Capacity = @Capacity,
                                    ModifiedDate = GETDATE()
                                WHERE ClassID = @ClassID";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClassID", classId);
                        command.Parameters.AddWithValue("@ClassName", className);
                        command.Parameters.AddWithValue("@Section", section);
                        command.Parameters.AddWithValue("@Capacity", capacity);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                SuccessMessage = "Class updated successfully!";
                await LoadClassesAsync();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating class: {ex.Message}";
                await LoadClassesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int classId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "DELETE FROM Classes WHERE ClassID = @ClassID";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClassID", classId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                SuccessMessage = "Class deleted successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting class: {ex.Message}";
            }

            await LoadClassesAsync();
            return Page();
        }

        private async Task LoadClassesAsync()
        {
            ClassList.Clear();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT ClassID, ClassName, Section, Capacity FROM Classes ORDER BY ClassName, Section";
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                ClassList.Add(new ClassInfo
                                {
                                    ClassID = reader.GetInt32(0),
                                    ClassName = reader.GetString(1),
                                    Section = reader.GetString(2),
                                    Capacity = reader.GetInt32(3)
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

    public class ClassInfo
    {
        public int ClassID { get; set; }
        public string ClassName { get; set; }
        public string Section { get; set; }
        public int Capacity { get; set; }
    }
}
