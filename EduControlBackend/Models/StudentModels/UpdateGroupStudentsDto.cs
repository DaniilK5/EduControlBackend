using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.StudentModels
{
    public class UpdateGroupStudentsDto
    {
        [Required(ErrorMessage = "Список ID студентов обязателен")]
        public List<int> StudentIds { get; set; } = new();
    }
}