using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace School_Management_System.Pages
{
    [Authorize(Roles = "Administrator")]
    public class AdminDashboardModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
