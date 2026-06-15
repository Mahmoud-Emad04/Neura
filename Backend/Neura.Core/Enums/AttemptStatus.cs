namespace Neura.Core.Enums;

public enum AttemptStatus
{
    InProgress = 0,
    Submitted = 1,
    TimedOut = 2,
    AutoSubmitted = 3,
    Graded = 4,
    ViolationFlagged = 5,
    Resolved = 6
}