namespace Neura.Core.Entities;

public class CourseLearningOutcome
{
    public string Outcome { get; set; } = string.Empty; // e.g., "Master Python for Data Science"

    // Foreign Key
    public int CourseId { get; set; }
}