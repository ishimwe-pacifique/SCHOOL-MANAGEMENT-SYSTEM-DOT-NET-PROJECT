using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystems.Models;

namespace SchoolManagementSystems.Data
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
