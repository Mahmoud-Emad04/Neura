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
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Endin < Startin)
            yield return new ValidationResult("End date must be the same or after the start date.", new[] { nameof(Endin), nameof(Startin) });

        if (Tags is null || !Tags.Any())
            yield return new ValidationResult("At least one tag must be provided.", new[] { nameof(Tags) });
    }
}