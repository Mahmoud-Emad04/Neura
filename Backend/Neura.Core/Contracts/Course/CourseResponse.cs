using Neura.Core.Contracts.Section;

namespace Neura.Core.Contracts.Course;

public record CourseResponse(string KeyId, int TotalHours, int TotalLessons, List<SectionResponse>? Sections);