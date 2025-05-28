using EduControlBackend.Models;
using EduControlBackend.Models.AdminModels;
using EduControlBackend.Models.Chat;
using EduControlBackend.Models.LoginAndReg;
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
        public DbSet<GroupChat> GroupChats { get; set; }
        public DbSet<GroupChatMember> GroupChatMembers { get; set; }
        public DbSet<AppSettings> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.GroupChat)
                .WithMany(gc => gc.Messages)
                .HasForeignKey(m => m.GroupChatId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            modelBuilder.Entity<GroupChatMember>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupChatMember>()
                .HasOne(m => m.GroupChat)
                .WithMany(gc => gc.Members)
                .HasForeignKey(m => m.GroupChatId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
