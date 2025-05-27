using EduControlBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EduControlBackend
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Grades> Grades { get; set; }
        public DbSet<Message> Messages { get; set; }
    }
}
