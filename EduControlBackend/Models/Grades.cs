using EduControlBackend.Models.LoginAndReg;

namespace EduControlBackend.Models
{
    public class Grades
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; }
        public int StudentId { get; set; }
        public User Student { get; set; }
        public string Grade { get; set; }
        public string Comment { get; set; }
    }
}
