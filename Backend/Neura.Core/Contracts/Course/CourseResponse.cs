using Neura.Core.Contracts.Section;

namespace Neura.Core.Contracts.Course;

public record CourseResponse
{
    public string KeyId { get; set; } = string.Empty;
    public int Hours { get; set; }
    public List<SectionResponse>? Sections { get; set; } = [];
}