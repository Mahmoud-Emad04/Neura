namespace Neura.Core.Contracts.Course;

public record CourseFullContentResponse(
    int CourseId,
    string CourseTitle,
    List<string> LearningOutcomes,
    List<string> Prerequisites,
    List<CourseFullContentSectionResponse> Sections
);

public record CourseFullContentSectionResponse(
    int SectionId,
    string SectionTitle,
    string? SectionDescription,
    List<CourseFullContentLessonResponse> Lessons
);

public record CourseFullContentLessonResponse(
    int LessonId,
    string LessonTitle,
    string? LessonDescription,
    string? LessonText
);
