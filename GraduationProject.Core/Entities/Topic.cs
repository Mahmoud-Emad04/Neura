namespace GraduationProject.Core.Entities;

public class Topic
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;
}