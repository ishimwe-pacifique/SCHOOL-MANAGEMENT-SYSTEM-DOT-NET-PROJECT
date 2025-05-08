using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SCHOOLMGTSYSTEM.Pages
{
    public class TeacherDashboardModel : PageModel
    {
        // You can add properties here to pass data to the view
        public string TeacherName { get; set; }
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }

        public void OnGet()
        {
            // You can initialize properties or fetch data from a database here
            TeacherName = "John Doe"; // Example: This would typically come from authentication/session
            TotalClasses = 5; // Example: This would typically come from database
            TotalStudents = 150; // Example: This would typically come from database
        }
    }
}