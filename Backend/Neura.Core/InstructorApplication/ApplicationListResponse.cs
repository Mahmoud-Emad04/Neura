using Neura.Core.Enums;

namespace Neura.Core.InstructorApplication;

public class ApplicationListResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public string StatusText => Status.ToString();
    public DateTime CreatedOn { get; set; }
    public DateTime? ReviewedOn { get; set; }
}