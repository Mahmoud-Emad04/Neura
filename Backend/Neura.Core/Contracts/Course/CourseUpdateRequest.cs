using System.ComponentModel.DataAnnotations;

namespace Neura.Core.Contracts.Course;

public record CourseUpdateRequest(
    [Required]
    string Title,
    string Description,
    DateOnly Startin,
    DateOnly Endin,
    List<int> Tags) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
            yield return new ValidationResult("Title is required.", new[] { nameof(Title) });

        if (Endin < Startin)
            yield return new ValidationResult("End date must be the same or after the start date.", new[] { nameof(Endin), nameof(Startin) });
    }
}