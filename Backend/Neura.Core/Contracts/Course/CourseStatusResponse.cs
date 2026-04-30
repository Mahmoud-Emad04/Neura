using Neura.Core.Enums;

namespace Neura.Core.Contracts.Courses;

public sealed record CourseStatusResponse
{
    public string KeyId { get; init; } = string.Empty;
    public CourseStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public bool IsEnrollmentOpen { get; init; }
    public bool IsAccessibleToStudents { get; init; }
    public bool CanActivate { get; init; }
    public bool CanComplete { get; init; }
    public bool CanReactivate { get; init; }
    public bool CanUnpublish { get; init; }
    public ActivationRequirements? Requirements { get; init; }
}

public sealed record ActivationRequirements
{
    public bool HasSections { get; init; }
    public bool HasLessons { get; init; }
    public bool HasPublishedLessons { get; init; }
    public int TotalSections { get; init; }
    public int TotalLessons { get; init; }
    public int PublishedLessons { get; init; }
    public bool CanActivate => HasPublishedLessons;
    public List<string> MissingRequirements { get; init; } = [];
}