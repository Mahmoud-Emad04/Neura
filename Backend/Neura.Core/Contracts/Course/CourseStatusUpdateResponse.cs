using Neura.Core.Enums;

namespace Neura.Core.Contracts.Courses;

public sealed record CourseStatusUpdateResponse
{
    public string KeyId { get; init; } = string.Empty;
    public CourseStatus PreviousStatus { get; init; }
    public CourseStatus CurrentStatus { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
}