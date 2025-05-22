using School_Management_System.Pages;
using Microsoft.EntityFrameworkCore; // Add this namespace
using School_Management_System.Models; // Reference where User class lives

namespace School_Management_System.Data
{
    public class SchoolDBContext : DbContext
    {
        public SchoolDBContext(DbContextOptions<SchoolDBContext> options)
          : base(options) // Now valid because we inherit from DbContext
        {
        }

        // 3. DbSet will now work with proper inheritance
        public DbSet<User> Users { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Attendance> Attendance { get; set; }
        
    }
}
