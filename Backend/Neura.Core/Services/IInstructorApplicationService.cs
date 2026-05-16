using Neura.Core.Abstractions;
using Neura.Core.Enums;
using Neura.Core.InstructorApplication;

namespace Neura.Core.Services;

public interface IInstructorApplicationService
{
    /// <summary>
    ///     Submit a new instructor application
    /// </summary>
    Task<Result<ApplicationResponse>> SubmitApplicationAsync(string userId, SubmitApplicationRequest request);

    /// <summary>
    ///     Get current user's application status
    /// </summary>
    Task<Result<MyApplicationStatusResponse>> GetMyApplicationStatusAsync(string userId);

    /// <summary>
    ///     Update a pending application
    /// </summary>
    Task<Result<ApplicationResponse>> UpdateApplicationAsync(string userId, UpdateApplicationRequest request);

    /// <summary>
    ///     Get application by ID (Admin)
    /// </summary>
    Task<Result<ApplicationResponse>> GetApplicationByIdAsync(int applicationId);

    /// <summary>
    ///     Get all applications with optional filtering (Admin)
    /// </summary>
    Task<Result<PaginatedList<ApplicationListResponse>>> GetApplicationsAsync(
        ApplicationStatus? status = null,
        int pageNumber = 1,
        int pageSize = 10);

    /// <summary>
    ///     Approve an application (Admin)
    /// </summary>
    Task<Result<ApplicationResponse>> ApproveApplicationAsync(int applicationId, string reviewerId);

    /// <summary>
    ///     Reject an application (Admin)
    /// </summary>
    Task<Result<ApplicationResponse>> RejectApplicationAsync(int applicationId, string reviewerId,
        ReviewApplicationRequest request);
}