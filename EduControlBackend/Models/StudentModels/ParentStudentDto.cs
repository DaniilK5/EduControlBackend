namespace EduControlBackend.Models.StudentModels
{
    public class AddParentDto
    {
        public int StudentId { get; set; }
        public int ParentId { get; set; }
    }

    public class UpdateParentStudentsDto
    {
        public int ParentId { get; set; }
        public List<int> StudentIds { get; set; } = new List<int>();
    }

    public class UpdateStudentParentsDto
    {
        public int StudentId { get; set; }
        public List<int> ParentIds { get; set; } = new List<int>();
    }
}