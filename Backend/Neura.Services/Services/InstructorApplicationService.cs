using Neura.Core.Abstractions.Consts;
using Neura.Core.Enums;
using Neura.Core.InstructorApplication;

namespace Neura.Services.Services;

public class InstructorApplicationService : IInstructorApplicationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InstructorApplicationService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public InstructorApplicationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<InstructorApplicationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<ApplicationResponse>> SubmitApplicationAsync(
        string userId,
        SubmitApplicationRequest request)
    {
        // Get user
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null) return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.UserNotFound);

        // Check if already instructor
        if (await _userManager.IsInRoleAsync(user, DefaultRoles.Instructor))
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.AlreadyInstructor);

        // Check for existing pending application
        var existingApplication = await _context.InstructorApplications
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedOn)
            .FirstOrDefaultAsync();

        if (existingApplication is not null)
        {
            if (existingApplication.Status == ApplicationStatus.Pending)
                return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.PendingApplicationExists);

            // Check reapply date for rejected applications
            if (existingApplication.Status == ApplicationStatus.Rejected &&
                existingApplication.CanReapplyAfter.HasValue &&
                DateTime.UtcNow < existingApplication.CanReapplyAfter.Value)
                return Result.Failure<ApplicationResponse>(
                    InstructorApplicationErrors.ReapplyDateNotReached(existingApplication.CanReapplyAfter.Value));
        }

        // Create new application
        var application = new InstructorApplication
        {
            UserId = userId,
            Bio = request.Bio.Trim(),
            Experience = request.Experience.Trim(),
            Status = ApplicationStatus.Pending,
            CreatedOn = DateTime.UtcNow
        };

        _context.InstructorApplications.Add(application);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} submitted instructor application {ApplicationId}",
            userId, application.Id);

        return Result.Success(MapToResponse(application, user));
    }

    public async Task<Result<MyApplicationStatusResponse>> GetMyApplicationStatusAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null) return Result.Failure<MyApplicationStatusResponse>(InstructorApplicationErrors.UserNotFound);

        var isInstructor = await _userManager.IsInRoleAsync(user, DefaultRoles.Instructor);

        if (isInstructor)
            return Result.Success(new MyApplicationStatusResponse
            {
                HasApplication = false,
                IsInstructor = true,
                CanApply = false,
                Message = "You are already an approved instructor"
            });

        var latestApplication = await _context.InstructorApplications
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedOn)
            .FirstOrDefaultAsync();

        if (latestApplication is null)
            return Result.Success(new MyApplicationStatusResponse
            {
                HasApplication = false,
                IsInstructor = false,
                CanApply = true,
                Message = "You can apply to become an instructor"
            });

        var canApply = latestApplication.Status == ApplicationStatus.Rejected &&
                       (!latestApplication.CanReapplyAfter.HasValue ||
                        DateTime.UtcNow >= latestApplication.CanReapplyAfter.Value);

        return Result.Success(new MyApplicationStatusResponse
        {
            HasApplication = true,
            IsInstructor = false,
            CanApply = canApply,
            ApplicationId = latestApplication.Id,
            Status = latestApplication.Status,
            RejectionReason = latestApplication.RejectionReason,
            CreatedOn = latestApplication.CreatedOn,
            ReviewedOn = latestApplication.ReviewedOn,
            CanReapplyAfter = latestApplication.CanReapplyAfter,
            Message = GetStatusMessage(latestApplication.Status, latestApplication.CanReapplyAfter)
        });
    }

    public async Task<Result<ApplicationResponse>> UpdateApplicationAsync(
        string userId,
        UpdateApplicationRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null) return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.UserNotFound);

        var application = await _context.InstructorApplications
            .Where(a => a.UserId == userId && a.Status == ApplicationStatus.Pending)
            .FirstOrDefaultAsync();

        if (application is null)
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.ApplicationNotFound);

        application.Bio = request.Bio.Trim();
        application.Experience = request.Experience.Trim();

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated application {ApplicationId}", userId, application.Id);

        return Result.Success(MapToResponse(application, user));
    }

    public async Task<Result<ApplicationResponse>> GetApplicationByIdAsync(int applicationId)
    {
        var application = await _context.InstructorApplications
            .Include(a => a.User)
            .Include(a => a.ReviewedBy)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.ApplicationNotFound);

        return Result.Success(MapToResponse(application, application.User, application.ReviewedBy));
    }

    public async Task<Result<PaginatedList<ApplicationListResponse>>> GetApplicationsAsync(
        ApplicationStatus? status = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var query = _context.InstructorApplications
            .Include(a => a.User)
            .AsQueryable();

        if (status.HasValue) query = query.Where(a => a.Status == status.Value);

        query = query.OrderByDescending(a => a.CreatedOn);

        var projectedQuery = query.Select(a => new ApplicationListResponse
        {
            Id = a.Id,
            UserId = a.UserId,
            UserName = $"{a.User.FirstName} {a.User.LastName}",
            UserEmail = a.User.Email ?? string.Empty,
            Status = a.Status,
            CreatedOn = a.CreatedOn,
            ReviewedOn = a.ReviewedOn
        });

        var paginatedList = await PaginatedList<ApplicationListResponse>.CreateAsync(
            projectedQuery, pageNumber, pageSize);

        return Result.Success(paginatedList);
    }

    public async Task<Result<ApplicationResponse>> ApproveApplicationAsync(
        int applicationId,
        string reviewerId)
    {
        var application = await _context.InstructorApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.ApplicationNotFound);

        if (application.Status != ApplicationStatus.Pending)
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.ApplicationAlreadyReviewed);

        var reviewer = await _userManager.FindByIdAsync(reviewerId);

        // Update application
        application.Status = ApplicationStatus.Approved;
        application.ReviewedById = reviewerId;
        application.ReviewedOn = DateTime.UtcNow;

        // Add instructor role to user
        var user = application.User;
        var roleResult = await _userManager.AddToRoleAsync(user, DefaultRoles.Instructor);

        if (!roleResult.Succeeded)
        {
            _logger.LogError("Failed to add Instructor role to user {UserId}: {Errors}",
                user.Id, string.Join(", ", roleResult.Errors.Select(e => e.Description)));

            return Result.Failure<ApplicationResponse>(
                new Error("InstructorApplication.RoleAssignmentFailed",
                    "Failed to assign instructor role",
                    StatusCodes.Status409Conflict));
        }

        // Update user's instructor status
        user.InstructorApprovedOn = DateTime.UtcNow;
        user.Bio = application.Bio;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Application {ApplicationId} approved by {ReviewerId}. User {UserId} is now an instructor",
            applicationId, reviewerId, user.Id);

        return Result.Success(MapToResponse(application, user, reviewer));
    }

    public async Task<Result<ApplicationResponse>> RejectApplicationAsync(
        int applicationId,
        string reviewerId,
        ReviewApplicationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RejectionReason))
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.RejectionReasonRequired);

        var application = await _context.InstructorApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.ApplicationNotFound);

        if (application.Status != ApplicationStatus.Pending)
            return Result.Failure<ApplicationResponse>(InstructorApplicationErrors.ApplicationAlreadyReviewed);

        var reviewer = await _userManager.FindByIdAsync(reviewerId);

        // Update application
        application.Status = ApplicationStatus.Rejected;
        application.ReviewedById = reviewerId;
        application.ReviewedOn = DateTime.UtcNow;
        application.RejectionReason = request.RejectionReason.Trim();
        application.CanReapplyAfter = DateTime.UtcNow.AddDays(CourseLimits.ReapplyWaitDays);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Application {ApplicationId} rejected by {ReviewerId}",
            applicationId, reviewerId);

        return Result.Success(MapToResponse(application, application.User, reviewer));
    }

    #region Private Helpers

    private static ApplicationResponse MapToResponse(
        InstructorApplication application,
        ApplicationUser user,
        ApplicationUser? reviewer = null)
    {
        return new ApplicationResponse
        {
            Id = application.Id,
            UserId = application.UserId,
            UserName = $"{user.FirstName} {user.LastName}",
            UserEmail = user.Email ?? string.Empty,
            Status = application.Status,
            Bio = application.Bio,
            Experience = application.Experience,
            RejectionReason = application.RejectionReason,
            CreatedOn = application.CreatedOn,
            ReviewedOn = application.ReviewedOn,
            ReviewedByName = reviewer is not null ? $"{reviewer.FirstName} {reviewer.LastName}" : null,
            CanReapplyAfter = application.CanReapplyAfter
        };
    }

    private static string GetStatusMessage(ApplicationStatus status, DateTime? canReapplyAfter)
    {
        return status switch
        {
            ApplicationStatus.Pending => "Your application is under review",
            ApplicationStatus.Approved => "Congratulations! Your application has been approved",
            ApplicationStatus.Rejected when canReapplyAfter.HasValue && DateTime.UtcNow < canReapplyAfter.Value =>
                $"Your application was rejected. You can reapply after {canReapplyAfter.Value:yyyy-MM-dd}",
            ApplicationStatus.Rejected => "Your application was rejected. You can submit a new application",
            _ => string.Empty
        };
    }

    #endregion
}