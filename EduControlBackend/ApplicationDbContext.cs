using EduControlBackend.Models;
using EduControlBackend.Models.AdminModels;
using EduControlBackend.Models.AssignmentModels;
using EduControlBackend.Models.Chat;
using EduControlBackend.Models.CourseModels;
using EduControlBackend.Models.GradeModels;
using EduControlBackend.Models.LoginAndReg;
using EduControlBackend.Models.StudentModels;
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
        public DbSet<Grade> Grades { get; set; } // Оставляем только одну сущность для оценок
        public DbSet<Message> Messages { get; set; }
        public DbSet<GroupChat> GroupChats { get; set; }
        public DbSet<GroupChatMember> GroupChatMembers { get; set; }
        public DbSet<AppSettings> Settings { get; set; }
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
        public DbSet<Subject> Subjects { get; set; } // Добавляем DbSet для предметов
        public DbSet<StudentGroup> StudentGroups { get; set; }
        public DbSet<GradeImage> GradeImages { get; set; }
        public DbSet<Absence> Absences { get; set; }


        public DbSet<CourseStudent> CourseStudents { get; set; }
        public DbSet<CourseTeacher> CourseTeachers { get; set; }

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

            // Конфигурация для CourseTeacher
            modelBuilder.Entity<CourseTeacher>()
                .HasKey(ct => new { ct.CourseId, ct.UserId });

            modelBuilder.Entity<CourseTeacher>()
                .HasOne(ct => ct.Course)
                .WithMany(c => c.Teachers)
                .HasForeignKey(ct => ct.CourseId);

            modelBuilder.Entity<CourseTeacher>()
                .HasOne(ct => ct.User)
                .WithMany()
                .HasForeignKey(ct => ct.UserId);

            modelBuilder.Entity<CourseTeacher>()
                .ToTable("CourseTeacher"); // Явно указываем имя таблицы

            // Конфигурация для CourseStudent
            modelBuilder.Entity<CourseStudent>()
                .HasKey(cs => new { cs.CourseId, cs.UserId });

            modelBuilder.Entity<CourseStudent>()
                .HasOne(cs => cs.Course)
                .WithMany(c => c.Students)
                .HasForeignKey(cs => cs.CourseId);

            modelBuilder.Entity<CourseStudent>()
                .HasOne(cs => cs.User)
                .WithMany()
                .HasForeignKey(cs => cs.UserId);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Assignment)
                .WithMany()
                .HasForeignKey(g => g.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Student)
                .WithMany()
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Instructor)
                .WithMany()
                .HasForeignKey(g => g.InstructorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Submission)
                .WithOne(s => s.Grade)
                .HasForeignKey<Grade>(g => g.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Конфигурация для Subject
            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.Code)
                .IsUnique();

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Subject)
                .WithMany(s => s.Courses)
                .HasForeignKey(c => c.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Конфигурация для StudentGroup
            modelBuilder.Entity<StudentGroup>()
                .HasOne(g => g.Curator)
                .WithMany(u => u.CuratedGroups)
                .HasForeignKey(g => g.CuratorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Group)
                .WithMany(g => g.Students)
                .HasForeignKey(u => u.StudentGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<StudentGroup>()
                .HasIndex(g => g.Name)
                .IsUnique();

            modelBuilder.Entity<StudentGroup>()
                .Property(g => g.Name)
                .HasMaxLength(10) // Ограничиваем длину названия
                .IsRequired();

            modelBuilder.Entity<GradeImage>()
                .HasOne(g => g.Subject)
                .WithMany()
                .HasForeignKey(g => g.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GradeImage>()
                .HasOne(g => g.StudentGroup)
                .WithMany()
                .HasForeignKey(g => g.StudentGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GradeImage>()
                .HasOne(g => g.Uploader)
                .WithMany()
                .HasForeignKey(g => g.UploaderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Absence>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Absence>()
                .HasOne(a => a.StudentGroup)
                .WithMany()
                .HasForeignKey(a => a.StudentGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Absence>()
                .HasOne(a => a.Instructor)
                .WithMany()
                .HasForeignKey(a => a.InstructorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Конфигурация связи родитель-студент
            modelBuilder.Entity<User>()
                .HasMany(u => u.Parents)
                .WithMany(u => u.Children)
                .UsingEntity(j => j.ToTable("ParentStudentRelations"));
        }
    }
}
