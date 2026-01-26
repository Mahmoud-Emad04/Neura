using System.ComponentModel.DataAnnotations;

namespace Neura.Core.Contracts.Course;

public record CourseUpdateRequest(
    [Required]
    string Title,
    string Description,
    DateOnly Startin,
    DateOnly Endin,
    List<int> Tags);