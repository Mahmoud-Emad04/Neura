using Neura.Core.Enums;

namespace Neura.Core.InstructorApplication;

public class MyApplicationStatusResponse
{
    public bool HasApplication { get; set; }
    public bool IsInstructor { get; set; }
    public bool CanApply { get; set; }
    public int? ApplicationId { get; set; }
    public ApplicationStatus? Status { get; set; }
    public string? StatusText => Status?.ToString();
    public string? RejectionReason { get; set; }
    public string? Bio { get; set; }
    public string? Experience { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? ReviewedOn { get; set; }
    public DateTime? CanReapplyAfter { get; set; }
    public string? Message { get; set; }
}