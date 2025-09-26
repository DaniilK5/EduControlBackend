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

        public static class Policies
        {
            // Политики для администрирования
            public const string ManageUsers = "ManageUsers";
            public const string ManageSettings = "ManageSettings";

            // Политики для управления расписанием
            public const string ManageSchedule = "ManageSchedule";
            public const string ViewSchedule = "ViewSchedule";

            // Политики для оценок
            public const string ManageGrades = "ManageGrades";
            public const string ViewGrades = "ViewGrades";

            // Политики для сообщений
            public const string SendMessages = "SendMessages";
            public const string DeleteMessages = "DeleteMessages";
            public const string ManageGroupChats = "ManageGroupChats";

            // Политики для отчетов
            public const string ManageReports = "ManageReports";
            public const string ViewReports = "ViewReports";

            // Политики для курсов
            public const string ManageCourses = "ManageCourses";
            public const string ViewCourses = "ViewCourses";
            
            // Политики для заданий
            public const string ManageAssignments = "ManageAssignments";
            public const string ViewAssignments = "ViewAssignments";
            public const string SubmitAssignments = "SubmitAssignments";


            //политики для курирования

            public const string ManageStudents = "ManageStudents";
            public const string ViewStudentDetails = "ViewStudentDetails";

        }
    }
}