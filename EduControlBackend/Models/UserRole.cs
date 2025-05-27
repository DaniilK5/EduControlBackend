namespace EduControlBackend.Models
{
    public static class UserRole
    {
        public const string Administrator = "Administrator";
        public const string Parent = "Parent";
        public const string Teacher = "Teacher";
        public const string Student = "Student";

        public static readonly string[] AllRoles = new[]
        {
            Administrator,
            Parent,
            Teacher,
            Student
        };
    }
}