using Neura.Core.Enums;

namespace Neura.Core.InstructorApplication;

public class ApplicationResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public string StatusText => Status.ToString();
    public string Bio { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ReviewedOn { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? CanReapplyAfter { get; set; }
}