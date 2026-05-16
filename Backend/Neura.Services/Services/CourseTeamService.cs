using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Enums;

namespace Neura.Services.Services;

public class CourseTeamService : ICourseTeamService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CourseTeamService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public CourseTeamService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<CourseTeamService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    #region Team Management

    public async Task<Result<TeamOverviewResponse>> GetTeamOverviewAsync(int courseId, string requesterId)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

        if (course is null) return Result.Failure<TeamOverviewResponse>(CourseTeamErrors.CourseNotFound);

        var members = await GetTeamMembersInternalAsync(courseId);
        var pendingInvitations = await GetPendingInvitationsInternalAsync(courseId);

        // Count only non-student members for team limit
        var teamMemberCount = members.Count(m => m.RoleLevel >= (int)CourseRoleType.Assistant);

        return Result.Success(new TeamOverviewResponse
        {
            CourseId = courseId,
            CourseName = course.Title,
            TotalMembers = teamMemberCount,
            MaxMembers = CourseLimits.MaxTeamMembers,
            CanInviteMore = teamMemberCount < CourseLimits.MaxTeamMembers,
            Members = members,
            PendingInvitations = pendingInvitations
        });
    }

    public async Task<Result<List<TeamMemberResponse>>> GetTeamMembersAsync(int courseId)
    {
        var courseExists = await _context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == courseId && !c.IsDeleted);

        if (!courseExists) return Result.Failure<List<TeamMemberResponse>>(CourseTeamErrors.CourseNotFound);

        var members = await GetTeamMembersInternalAsync(courseId);
        return Result.Success(members);
    }

    public async Task<Result<TeamMemberResponse>> GetTeamMemberAsync(int courseId, string userId)
    {
        var member = await _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.User)
            .Include(cu => cu.CourseRole)
            .Include(cu => cu.EnrolledBy)
            .Where(cu => cu.CourseId == courseId && cu.UserId == userId && !cu.IsDeleted)
            .FirstOrDefaultAsync();

        if (member is null) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.MemberNotFound);

        return Result.Success(MapToTeamMemberResponse(member));
    }

    public async Task<Result> RemoveTeamMemberAsync(int courseId, string userId, string requesterId)
    {
        // Cannot remove self
        if (userId == requesterId) return Result.Failure(CourseTeamErrors.CannotRemoveSelf);

        var requester = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == requesterId && !cu.IsDeleted);

        if (requester is null) return Result.Failure(CourseTeamErrors.InsufficientPermission);

        // Check if requester has ManageTeam permission
        if (!CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageTeam))
            return Result.Failure(CourseTeamErrors.InsufficientPermission);

        var targetMember = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId && !cu.IsDeleted);

        if (targetMember is null) return Result.Failure(CourseTeamErrors.MemberNotFound);

        // Cannot remove owner
        if (targetMember.CourseRole.Level == (int)CourseRoleType.CourseOwner)
            return Result.Failure(CourseTeamErrors.CannotRemoveOwner);

        // Cannot manage users with equal or higher role (unless owner)
        if (requester.CourseRole.Level != (int)CourseRoleType.CourseOwner &&
            targetMember.CourseRole.Level >= requester.CourseRole.Level)
            return Result.Failure(CourseTeamErrors.CannotManageHigherRole);

        // Soft delete
        targetMember.IsDeleted = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {RequesterId} removed {UserId} from course {CourseId}",
            requesterId, userId, courseId);

        return Result.Success();
    }

    public async Task<Result<TeamMemberResponse>> ChangeTeamRoleAsync(
        int courseId,
        string userId,
        ChangeTeamRoleRequest request,
        string requesterId)
    {
        // Get the course role entity
        var newRole = await _context.CourseRoles
            .FirstOrDefaultAsync(r => r.Level == (int)request.NewRole);

        if (newRole is null) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InvalidRole);

        // Cannot assign owner role via this method
        if (request.NewRole == CourseRoleType.CourseOwner)
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.CannotAssignOwnerRole);

        var requester = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == requesterId && !cu.IsDeleted);

        if (requester is null) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InsufficientPermission);

        // Check if requester has ManageTeam permission
        if (!CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageTeam))
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InsufficientPermission);

        var targetMember = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .Include(cu => cu.User)
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId && !cu.IsDeleted);

        if (targetMember is null) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.MemberNotFound);

        // Cannot change owner's role
        if (targetMember.CourseRole.Level == (int)CourseRoleType.CourseOwner)
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.CannotChangeOwnerRole);

        // Non-owners cannot manage users with equal or higher role
        if (requester.CourseRole.Level != (int)CourseRoleType.CourseOwner &&
            targetMember.CourseRole.Level >= requester.CourseRole.Level)
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.CannotManageHigherRole);

        // Update role
        targetMember.CourseRoleId = newRole.Id;
        targetMember.PermissionMask = newRole.PermissionMask;

        await _context.SaveChangesAsync();

        // Reload with updated role
        await _context.Entry(targetMember).Reference(cu => cu.CourseRole).LoadAsync();

        _logger.LogInformation("User {RequesterId} changed {UserId}'s role to {NewRole} in course {CourseId}",
            requesterId, userId, request.NewRole, courseId);

        return Result.Success(MapToTeamMemberResponse(targetMember));
    }

    public async Task<Result> TransferOwnershipAsync(
        int courseId,
        TransferOwnershipRequest request,
        string requesterId)
    {
        // Cannot transfer to self
        if (request.NewOwnerId == requesterId) return Result.Failure(CourseTeamErrors.TransferToSelf);

        var currentOwner = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == requesterId &&
                !cu.IsDeleted &&
                cu.CourseRole.Level == (int)CourseRoleType.CourseOwner);

        if (currentOwner is null) return Result.Failure(CourseTeamErrors.InsufficientPermission);

        var newOwner = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == request.NewOwnerId &&
                !cu.IsDeleted);

        if (newOwner is null) return Result.Failure(CourseTeamErrors.TransferTargetNotTeamMember);

        // Get role entities
        var ownerRole = await _context.CourseRoles
            .FirstAsync(r => r.Level == (int)CourseRoleType.CourseOwner);

        var coInstructorRole = await _context.CourseRoles
            .FirstAsync(r => r.Level == (int)CourseRoleType.CoInstructor);

        // Transfer ownership
        currentOwner.CourseRoleId = coInstructorRole.Id;
        currentOwner.PermissionMask = coInstructorRole.PermissionMask;

        newOwner.CourseRoleId = ownerRole.Id;
        newOwner.PermissionMask = ownerRole.PermissionMask;

        // Update course creator if tracked
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is not null) course.CreatedById = request.NewOwnerId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ownership of course {CourseId} transferred from {OldOwnerId} to {NewOwnerId}",
            courseId, requesterId, request.NewOwnerId);

        return Result.Success();
    }

    #endregion

    #region Private Helpers

    private async Task<List<TeamMemberResponse>> GetTeamMembersInternalAsync(int courseId)
    {
        return await _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.User)
            .Include(cu => cu.CourseRole)
            .Include(cu => cu.EnrolledBy)
            .Where(cu => cu.CourseId == courseId && !cu.IsDeleted)
            .OrderByDescending(cu => cu.CourseRole.Level)
            .ThenBy(cu => cu.EnrolledOn)
            .Select(cu => MapToTeamMemberResponse(cu))
            .ToListAsync();
    }

    private async Task<List<CourseInvitationResponse>> GetPendingInvitationsInternalAsync(int courseId)
    {
        return await _context.CourseInvitations
            .AsNoTracking()
            .Include(ci => ci.CourseRole)
            .Include(ci => ci.InvitedBy)
            .Include(ci => ci.Course)
            .Where(ci => ci.CourseId == courseId && ci.Status == InvitationStatus.Pending)
            .OrderByDescending(ci => ci.InvitedOn)
            .Select(ci => MapToInvitationResponse(ci))
            .ToListAsync();
    }

    private static TeamMemberResponse MapToTeamMemberResponse(CourseUser cu)
    {
        return new TeamMemberResponse
        {
            UserId = cu.UserId,
            FirstName = cu.User.FirstName,
            LastName = cu.User.LastName,
            Email = cu.User.Email ?? string.Empty,
            CourseRoleId = cu.CourseRoleId,
            RoleName = cu.CourseRole.Name,
            RoleType = (CourseRoleType)cu.CourseRole.Level,
            RoleLevel = cu.CourseRole.Level,
            PermissionMask = cu.PermissionMask,
            EnrolledOn = cu.EnrolledOn,
            EnrolledByName = cu.EnrolledBy is not null
                ? $"{cu.EnrolledBy.FirstName} {cu.EnrolledBy.LastName}"
                : null,
            LastAccessedOn = cu.LastAccessedOn,
            IsOwner = cu.CourseRole.Level == (int)CourseRoleType.CourseOwner
        };
    }

    private static CourseInvitationResponse MapToInvitationResponse(CourseInvitation ci)
    {
        return new CourseInvitationResponse
        {
            Id = ci.Id,
            CourseId = ci.CourseId,
            CourseName = ci.Course.Title,
            Email = ci.Email,
            RoleName = ci.CourseRole.Name,
            RoleType = (CourseRoleType)ci.CourseRole.Level,
            Status = ci.Status,
            CustomMessage = ci.CustomMessage,
            InvitedByName = $"{ci.InvitedBy.FirstName} {ci.InvitedBy.LastName}",
            InvitedOn = ci.InvitedOn,
            ExpiresOn = ci.ExpiresOn,
            RespondedOn = ci.RespondedOn
        };
    }

    #endregion

    // Add these methods to the CourseTeamService class

    #region Invitations - Course Owner Side

    public async Task<Result<CourseInvitationResponse>> InviteTeamMemberAsync(
        int courseId,
        InviteTeamMemberRequest request,
        string inviterId)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Validate course exists
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

        if (course is null) return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.CourseNotFound);

        // Validate requester has permission
        var requester = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == inviterId && !cu.IsDeleted);

        if (requester is null ||
            !CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageTeam))
            return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.InsufficientPermission);

        // Cannot invite self
        var inviterUser = await _userManager.FindByIdAsync(inviterId);
        if (inviterUser?.Email?.ToLowerInvariant() == normalizedEmail)
            return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.CannotInviteSelf);

        // Cannot assign owner role
        if (request.Role == CourseRoleType.CourseOwner)
            return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.CannotAssignOwnerRole);

        // Check team limit (only for non-student roles)
        if (request.Role >= CourseRoleType.Assistant)
        {
            var teamMemberCount = await _context.CourseUsers
                .Include(cu => cu.CourseRole)
                .CountAsync(cu =>
                    cu.CourseId == courseId &&
                    !cu.IsDeleted &&
                    cu.CourseRole.Level >= (int)CourseRoleType.Assistant);

            if (teamMemberCount >= CourseLimits.MaxTeamMembers)
                return Result.Failure<CourseInvitationResponse>(
                    CourseTeamErrors.TeamLimitReached(CourseLimits.MaxTeamMembers));
        }

        // Check if user already a team member
        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null)
        {
            var existingMembership = await _context.CourseUsers
                .AnyAsync(cu =>
                    cu.CourseId == courseId &&
                    cu.UserId == existingUser.Id &&
                    !cu.IsDeleted);

            if (existingMembership) return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.AlreadyTeamMember);
        }

        // Check for existing pending invitation
        var existingInvitation = await _context.CourseInvitations
            .AnyAsync(ci =>
                ci.CourseId == courseId &&
                ci.Email.ToLower() == normalizedEmail &&
                ci.Status == InvitationStatus.Pending);

        if (existingInvitation) return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.AlreadyInvited);

        // Get role entity
        var role = await _context.CourseRoles
            .FirstOrDefaultAsync(r => r.Level == (int)request.Role);

        if (role is null) return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.InvalidRole);

        // Create invitation
        var invitation = new CourseInvitation
        {
            CourseId = courseId,
            Email = normalizedEmail,
            Token = GenerateInvitationToken(),
            CourseRoleId = role.Id,
            Status = InvitationStatus.Pending,
            CustomMessage = request.CustomMessage?.Trim(),
            InvitedById = inviterId,
            InvitedOn = DateTime.UtcNow,
            ExpiresOn = DateTime.UtcNow.AddDays(CourseLimits.InvitationExpiryDays)
        };

        _context.CourseInvitations.Add(invitation);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(invitation).Reference(i => i.Course).LoadAsync();
        await _context.Entry(invitation).Reference(i => i.CourseRole).LoadAsync();
        await _context.Entry(invitation).Reference(i => i.InvitedBy).LoadAsync();

        _logger.LogInformation("User {InviterId} invited {Email} to course {CourseId} as {Role}",
            inviterId, normalizedEmail, courseId, request.Role);

        // TODO: Send invitation email

        return Result.Success(MapToInvitationResponse(invitation));
    }

    public async Task<Result<List<CourseInvitationResponse>>> GetPendingInvitationsAsync(int courseId)
    {
        var courseExists = await _context.Courses
            .AnyAsync(c => c.Id == courseId && !c.IsDeleted);

        if (!courseExists) return Result.Failure<List<CourseInvitationResponse>>(CourseTeamErrors.CourseNotFound);

        var invitations = await GetPendingInvitationsInternalAsync(courseId);
        return Result.Success(invitations);
    }

    public async Task<Result> CancelInvitationAsync(int courseId, int invitationId, string requesterId)
    {
        var requester = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == requesterId && !cu.IsDeleted);

        if (requester is null ||
            !CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageTeam))
            return Result.Failure(CourseTeamErrors.InsufficientPermission);

        var invitation = await _context.CourseInvitations
            .FirstOrDefaultAsync(ci =>
                ci.Id == invitationId &&
                ci.CourseId == courseId &&
                ci.Status == InvitationStatus.Pending);

        if (invitation is null) return Result.Failure(CourseTeamErrors.InvitationNotFound);

        invitation.Status = InvitationStatus.Cancelled;
        invitation.RespondedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Invitation {InvitationId} cancelled by {RequesterId}", invitationId, requesterId);

        return Result.Success();
    }

    public async Task<Result<CourseInvitationResponse>> ResendInvitationAsync(
        int courseId,
        int invitationId,
        string requesterId)
    {
        var requester = await _context.CourseUsers
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == requesterId && !cu.IsDeleted);

        if (requester is null ||
            !CoursePermissionMasks.HasPermission(requester.PermissionMask, CoursePermission.ManageTeam))
            return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.InsufficientPermission);

        var invitation = await _context.CourseInvitations
            .Include(ci => ci.Course)
            .Include(ci => ci.CourseRole)
            .Include(ci => ci.InvitedBy)
            .FirstOrDefaultAsync(ci =>
                ci.Id == invitationId &&
                ci.CourseId == courseId &&
                ci.Status == InvitationStatus.Pending);

        if (invitation is null) return Result.Failure<CourseInvitationResponse>(CourseTeamErrors.InvitationNotFound);

        // Extend expiry
        invitation.ExpiresOn = DateTime.UtcNow.AddDays(CourseLimits.InvitationExpiryDays);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Invitation {InvitationId} resent by {RequesterId}", invitationId, requesterId);

        // TODO: Resend invitation email

        return Result.Success(MapToInvitationResponse(invitation));
    }

    #endregion

    #region Invitations - Invitee Side

    public async Task<Result<InvitationDetailsResponse>> GetInvitationByTokenAsync(string token)
    {
        var invitation = await _context.CourseInvitations
            .AsNoTracking()
            .Include(ci => ci.Course)
            .Include(ci => ci.CourseRole)
            .Include(ci => ci.InvitedBy)
            .FirstOrDefaultAsync(ci => ci.Token == token);

        if (invitation is null) return Result.Failure<InvitationDetailsResponse>(CourseTeamErrors.InvitationNotFound);

        var response = new InvitationDetailsResponse
        {
            InvitationId = invitation.Id,
            Token = invitation.Token,
            CourseId = invitation.CourseId,
            CourseName = invitation.Course.Title,
            CourseDescription = invitation.Course.Description ?? string.Empty,
            CourseThumbnail = invitation.Course.ImageUrl,
            RoleName = invitation.CourseRole.Name,
            RoleDescription = invitation.CourseRole.Description,
            RoleType = (CourseRoleType)invitation.CourseRole.Level,
            CustomMessage = invitation.CustomMessage,
            InvitedByName = $"{invitation.InvitedBy.FirstName} {invitation.InvitedBy.LastName}",
            InvitedByEmail = invitation.InvitedBy.Email ?? string.Empty,
            InvitedOn = invitation.InvitedOn,
            ExpiresOn = invitation.ExpiresOn,
            Status = invitation.Status,
            IsValid = invitation.IsValid,
            InvalidReason = GetInvalidReason(invitation)
        };

        return Result.Success(response);
    }

    public async Task<Result<TeamMemberResponse>> AcceptInvitationAsync(string token, string userId)
    {
        var invitation = await _context.CourseInvitations
            .Include(ci => ci.Course)
            .Include(ci => ci.CourseRole)
            .FirstOrDefaultAsync(ci => ci.Token == token);

        if (invitation is null) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InvalidToken);

        if (invitation.Status != InvitationStatus.Pending)
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InvitationAlreadyResponded);

        if (DateTime.UtcNow >= invitation.ExpiresOn)
        {
            invitation.Status = InvitationStatus.Expired;
            await _context.SaveChangesAsync();
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InvitationExpired);
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.UserNotFound);

        // Verify email matches (case insensitive)
        if (user.Email?.ToLowerInvariant() != invitation.Email.ToLowerInvariant())
            return Result.Failure<TeamMemberResponse>(CourseTeamErrors.InvalidToken);

        // Check if already a member
        var existingMembership = await _context.CourseUsers
            .AnyAsync(cu =>
                cu.CourseId == invitation.CourseId &&
                cu.UserId == userId &&
                !cu.IsDeleted);

        if (existingMembership) return Result.Failure<TeamMemberResponse>(CourseTeamErrors.AlreadyTeamMember);

        // Create course membership
        var courseUser = new CourseUser
        {
            CourseId = invitation.CourseId,
            UserId = userId,
            CourseRoleId = invitation.CourseRoleId,
            PermissionMask = invitation.CourseRole.PermissionMask,
            EnrolledOn = DateTime.UtcNow,
            EnrolledById = invitation.InvitedById,
            InvitationId = invitation.Id
        };

        _context.CourseUsers.Add(courseUser);

        // Update invitation
        invitation.Status = InvitationStatus.Accepted;
        invitation.RespondedOn = DateTime.UtcNow;
        invitation.AcceptedUserId = userId;

        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(courseUser).Reference(cu => cu.User).LoadAsync();
        await _context.Entry(courseUser).Reference(cu => cu.CourseRole).LoadAsync();
        await _context.Entry(courseUser).Reference(cu => cu.EnrolledBy).LoadAsync();

        _logger.LogInformation("User {UserId} accepted invitation {InvitationId} for course {CourseId}",
            userId, invitation.Id, invitation.CourseId);

        return Result.Success(MapToTeamMemberResponse(courseUser));
    }

    public async Task<Result> RejectInvitationAsync(string token, string? userId = null)
    {
        var invitation = await _context.CourseInvitations
            .FirstOrDefaultAsync(ci => ci.Token == token);

        if (invitation is null) return Result.Failure(CourseTeamErrors.InvalidToken);

        if (invitation.Status != InvitationStatus.Pending)
            return Result.Failure(CourseTeamErrors.InvitationAlreadyResponded);

        // If userId provided, verify email matches
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.Email?.ToLowerInvariant() != invitation.Email.ToLowerInvariant())
                return Result.Failure(CourseTeamErrors.InvalidToken);
        }

        invitation.Status = InvitationStatus.Rejected;
        invitation.RespondedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Invitation {InvitationId} rejected", invitation.Id);

        return Result.Success();
    }

    public async Task<Result<MyInvitationsResponse>> GetMyInvitationsAsync(string userEmail)
    {
        var normalizedEmail = userEmail.ToLowerInvariant();

        var pendingInvitations = await _context.CourseInvitations
            .AsNoTracking()
            .Include(ci => ci.Course)
            .Include(ci => ci.CourseRole)
            .Include(ci => ci.InvitedBy)
            .Where(ci =>
                ci.Email.ToLower() == normalizedEmail &&
                ci.Status == InvitationStatus.Pending &&
                ci.ExpiresOn > DateTime.UtcNow)
            .OrderByDescending(ci => ci.InvitedOn)
            .Select(ci => MapToInvitationResponse(ci))
            .ToListAsync();

        return Result.Success(new MyInvitationsResponse
        {
            PendingInvitations = pendingInvitations,
            TotalPending = pendingInvitations.Count
        });
    }

    #endregion

    #region Additional Private Helpers

    private static string GenerateInvitationToken()
    {
        return $"{Guid.NewGuid():N}{Guid.NewGuid():N}"[..64];
    }

    private static string? GetInvalidReason(CourseInvitation invitation)
    {
        if (invitation.Status == InvitationStatus.Accepted)
            return "This invitation has already been accepted";

        if (invitation.Status == InvitationStatus.Rejected)
            return "This invitation has been rejected";

        if (invitation.Status == InvitationStatus.Cancelled)
            return "This invitation has been cancelled";

        if (invitation.Status == InvitationStatus.Expired || DateTime.UtcNow >= invitation.ExpiresOn)
            return "This invitation has expired";

        return null;
    }

    #endregion
}