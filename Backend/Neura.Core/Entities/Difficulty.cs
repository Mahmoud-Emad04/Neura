namespace Neura.Core.Entities;

public class Difficulty
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty!;

    public ICollection<Course> Courses { get; set; } = default!;
}