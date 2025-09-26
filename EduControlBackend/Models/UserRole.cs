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
            // �������� ��� �����������������
            public const string ManageUsers = "ManageUsers";
            public const string ManageSettings = "ManageSettings";

            // �������� ��� ���������� �����������
            public const string ManageSchedule = "ManageSchedule";
            public const string ViewSchedule = "ViewSchedule";

            // �������� ��� ������
            public const string ManageGrades = "ManageGrades";
            public const string ViewGrades = "ViewGrades";

            // �������� ��� ���������
            public const string SendMessages = "SendMessages";
            public const string DeleteMessages = "DeleteMessages";
            public const string ManageGroupChats = "ManageGroupChats";

            // �������� ��� �������
            public const string ManageReports = "ManageReports";
            public const string ViewReports = "ViewReports";

            // �������� ��� ������
            public const string ManageCourses = "ManageCourses";
            public const string ViewCourses = "ViewCourses";
            
            // �������� ��� �������
            public const string ManageAssignments = "ManageAssignments";
            public const string ViewAssignments = "ViewAssignments";
            public const string SubmitAssignments = "SubmitAssignments";


            //�������� ��� �����������

            public const string ManageStudents = "ManageStudents";
            public const string ViewStudentDetails = "ViewStudentDetails";

        }
    }
}