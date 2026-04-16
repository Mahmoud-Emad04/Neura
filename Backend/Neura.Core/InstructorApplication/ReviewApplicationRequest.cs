using System.ComponentModel.DataAnnotations;

namespace Neura.Core.InstructorApplication;

public class ReviewApplicationRequest
{
    [StringLength(1000, ErrorMessage = "Rejection reason cannot exceed 1000 characters")]
    public string? RejectionReason { get; set; }
}