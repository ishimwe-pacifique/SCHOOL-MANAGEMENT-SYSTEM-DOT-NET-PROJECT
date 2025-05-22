using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace School_Management_System.Pages
{
    [Authorize]
    public class SubjectManagementModel : PageModel
    {
        [BindProperty]
        public SubjectInputModel Subject { get; set; } = new SubjectInputModel();

        public List<SubjectInfo> Subjects { get; set; } = new List<SubjectInfo>();
        public List<SelectListItem> AvailableClasses { get; set; } = new List<SelectListItem>();
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        private readonly string _connectionString = "Data Source=DESKTOP-14GLE6P\\SQLEXPRESS;Initial Catalog=SchoolSysDB;Integrated Security=True;TrustServerCertificate=True";

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

                    // Get class info for the selected ClassID
                    var classInfo = await GetClassInfoById(connection, Subject.ClassID);
                    string baseClassName = GetBaseClassName(classInfo.ClassName);

                    // Add to selected class
                    await AddSubjectToClass(connection, Subject.SubjectName, Subject.SubjectCode, Subject.ClassID);

                    // If this is a base class (like P1), add to all sections
                    if (classInfo.ClassName == baseClassName)
                    {
                        var sectionClasses = await GetSectionClasses(connection, baseClassName);
                        foreach (var sectionClass in sectionClasses)
                        {
                            await AddSubjectToClass(connection, Subject.SubjectName, Subject.SubjectCode, sectionClass.ClassID);
                        }

                        SuccessMessage = $"Subject added successfully to {baseClassName} and all its sections!";
                    }
                    else
                    {
                        SuccessMessage = "Subject added successfully!";
                    }
                }

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

                    // Get current and new class info
                    var currentSubject = await GetSubjectById(connection, subjectId);
                    var newClassInfo = await GetClassInfoById(connection, classId);

                    string currentBaseName = GetBaseClassName(currentSubject.ClassName);
                    string newBaseName = GetBaseClassName(newClassInfo.ClassName);

                    // Update the subject
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

                    // Handle section synchronization
                    if (currentBaseName == newBaseName)
                    {
                        // Still in same base class - update all sections
                        if (currentSubject.ClassName == currentBaseName)
                        {
                            var sections = await GetSectionClasses(connection, currentBaseName);
                            foreach (var section in sections)
                            {
                                await UpdateSubjectByNameAndClass(connection, subjectName, subjectCode, section.ClassID);
                            }
                        }
                    }
                    else
                    {
                        // Changed base classes - remove from old sections, add to new
                        if (currentSubject.ClassName == currentBaseName)
                        {
                            var oldSections = await GetSectionClasses(connection, currentBaseName);
                            foreach (var section in oldSections)
                            {
                                await DeleteSubjectByNameAndClass(connection, currentSubject.SubjectName, section.ClassID);
                            }
                        }

                        if (newClassInfo.ClassName == newBaseName)
                        {
                            var newSections = await GetSectionClasses(connection, newBaseName);
                            foreach (var section in newSections)
                            {
                                await AddSubjectToClass(connection, subjectName, subjectCode, section.ClassID);
                            }
                        }
                    }

                    SuccessMessage = "Subject updated successfully!";
                }

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

                    // Get subject info before deleting
                    var subject = await GetSubjectById(connection, subjectId);
                    string baseClassName = GetBaseClassName(subject.ClassName);

                    // Delete the subject
                    var query = "DELETE FROM Subjects WHERE Subjectid = @SubjectID";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SubjectID", subjectId);
                        await command.ExecuteNonQueryAsync();
                    }

                    // If it was a base class subject, delete from all sections as well
                    if (subject.ClassName == baseClassName)
                    {
                        var sections = await GetSectionClasses(connection, baseClassName);
                        foreach (var section in sections)
                        {
                            await DeleteSubjectByNameAndClass(connection, subject.SubjectName, section.ClassID);
                        }
                    }

                    SuccessMessage = "Subject deleted successfully!";
                }
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
                                s.Classid, c.ClassName AS ClassDisplayName
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

                    // Get distinct base classes
                    var query = @"
                        SELECT 
                            MIN(ClassID) AS ClassID,
                            CASE 
                                WHEN ClassName LIKE '%[A-Z]' THEN LEFT(ClassName, PATINDEX('%[A-Z]%', ClassName + 'A') - 1)
                                ELSE ClassName
                            END AS BaseClassName
                        FROM Classes
                        GROUP BY 
                            CASE 
                                WHEN ClassName LIKE '%[A-Z]' THEN LEFT(ClassName, PATINDEX('%[A-Z]%', ClassName + 'A') - 1)
                                ELSE ClassName
                            END
                        ORDER BY BaseClassName";

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

        #region Helper Methods

        private async Task<ClassInfosub> GetClassInfoById(SqlConnection connection, int classId)
        {
            var query = "SELECT ClassID, ClassName FROM Classes WHERE ClassID = @ClassID";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ClassID", classId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new ClassInfosub
                        {
                            ClassID = reader.GetInt32(0),
                            ClassName = reader.GetString(1)
                        };
                    }
                }
            }
            return null;
        }

        private string GetBaseClassName(string className)
        {
            if (string.IsNullOrEmpty(className)) return className;

            // Remove any trailing letters (A, B, C, etc.)
            int lastNonLetter = className.Length;
            while (lastNonLetter > 0 && char.IsLetter(className[lastNonLetter - 1]))
            {
                lastNonLetter--;
            }

            return className.Substring(0, lastNonLetter);
        }

        private async Task<List<ClassInfo>> GetSectionClasses(SqlConnection connection, string baseClassName)
        {
            var sections = new List<ClassInfo>();
            var query = @"
                SELECT ClassID, ClassName 
                FROM Classes 
                WHERE ClassName LIKE @BaseClassName + '[A-Z]'
                ORDER BY ClassName";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@BaseClassName", baseClassName);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        sections.Add(new ClassInfo
                        {
                            ClassID = reader.GetInt32(0),
                            ClassName = reader.GetString(1)
                        });
                    }
                }
            }
            return sections;
        }

        private async Task AddSubjectToClass(SqlConnection connection, string subjectName, string subjectCode, int classId)
        {
            // Check if subject already exists for this class
            var checkQuery = "SELECT COUNT(*) FROM Subjects WHERE SubjectName = @SubjectName AND Classid = @ClassID";
            using (var checkCommand = new SqlCommand(checkQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@SubjectName", subjectName);
                checkCommand.Parameters.AddWithValue("@ClassID", classId);
                int count = (int)await checkCommand.ExecuteScalarAsync();

                if (count == 0)
                {
                    var insertQuery = @"INSERT INTO Subjects (SubjectName, SubjectCode, Classid)
                                     VALUES (@SubjectName, @SubjectCode, @ClassID)";
                    using (var insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@SubjectName", subjectName);
                        insertCommand.Parameters.AddWithValue("@SubjectCode", subjectCode);
                        insertCommand.Parameters.AddWithValue("@ClassID", classId);
                        await insertCommand.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        private async Task<SubjectInfo> GetSubjectById(SqlConnection connection, int subjectId)
        {
            var query = @"SELECT s.SubjectID, s.SubjectName, s.SubjectCode, s.Classid, c.ClassName
                        FROM Subjects s
                        JOIN Classes c ON s.Classid = c.ClassID
                        WHERE s.SubjectID = @SubjectID";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@SubjectID", subjectId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new SubjectInfo
                        {
                            SubjectID = reader.GetInt32(0),
                            SubjectName = reader.GetString(1),
                            SubjectCode = reader.GetString(2),
                            ClassID = reader.GetInt32(3),
                            ClassName = reader.GetString(4)
                        };
                    }
                }
            }
            return null;
        }

        private async Task DeleteSubjectByNameAndClass(SqlConnection connection, string subjectName, int classId)
        {
            if (classId > 0)
            {
                var query = "DELETE FROM Subjects WHERE SubjectName = @SubjectName AND Classid = @ClassID";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SubjectName", subjectName);
                    command.Parameters.AddWithValue("@ClassID", classId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task UpdateSubjectByNameAndClass(SqlConnection connection, string subjectName, string subjectCode, int classId)
        {
            if (classId > 0)
            {
                var query = @"UPDATE Subjects 
                            SET SubjectName = @SubjectName, 
                                SubjectCode = @SubjectCode,
                                ModifiedDate = GETDATE()
                            WHERE SubjectName = @SubjectName AND Classid = @ClassID";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SubjectName", subjectName);
                    command.Parameters.AddWithValue("@SubjectCode", subjectCode);
                    command.Parameters.AddWithValue("@ClassID", classId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        #endregion
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

    public class ClassInfosub
    {
        public int ClassID { get; set; }
        public string ClassName { get; set; }
    }
}
