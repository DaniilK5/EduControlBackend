using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.StudentModels
{
    public class AssignCuratorDto
    {
        [Required(ErrorMessage = "ID преподавателя обязателен")]
        public int TeacherId { get; set; }
    }
}