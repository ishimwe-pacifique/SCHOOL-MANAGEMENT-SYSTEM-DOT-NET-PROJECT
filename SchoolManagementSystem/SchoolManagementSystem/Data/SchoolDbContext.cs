using SchoolManagementSystem.Pages;
using Microsoft.EntityFrameworkCore; // Add this namespace
using SchoolManagementSystem.Models; // Reference where User class lives

namespace SchoolManagementSystem.Data
{
    public class SchoolDbContext : DbContext
    {
        public SchoolDbContext(DbContextOptions<SchoolDbContext> options)
           : base(options) // Now valid because we inherit from DbContext
        {
        }

        // 3. DbSet will now work with proper inheritance
        public DbSet<User> Users { get; set; }
    }
}
