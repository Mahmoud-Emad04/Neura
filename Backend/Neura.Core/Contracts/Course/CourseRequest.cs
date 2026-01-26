using System.ComponentModel.DataAnnotations;

namespace Neura.Core.Contracts.Course;

public record CourseRequest(
    [Required]
    string Title,
    [Required]
    string InstructorName,
    [Required]
    string Description,
    int DifficultyId,
    [Range(0, int.MaxValue, ErrorMessage = "Price must be non-negative.")]
    int Price,
    DateOnly Startin,
    DateOnly Endin,
    List<int> Tags
);