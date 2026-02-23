namespace Neura.Core.Entities;

public class CoursePrerequisite
{
    public string Requirement { get; set; } = string.Empty;

    // Foreign Key
    public int CourseId { get; set; }
}