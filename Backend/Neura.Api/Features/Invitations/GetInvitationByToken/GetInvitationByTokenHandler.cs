using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.CourseTeam;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Api.Features.CourseTeam;

namespace Neura.Api.Features.Invitations.GetInvitationByToken;

internal sealed class GetInvitationByTokenHandler(ApplicationDbContext context) 
    : IRequestHandler<GetInvitationByTokenQuery, Result<InvitationDetailsResponse>>
{
    public async Task<Result<InvitationDetailsResponse>> Handle(
        GetInvitationByTokenQuery query, CancellationToken ct)
    {
        var invitation = await context.CourseInvitations
            .AsNoTracking()
            .Include(ci => ci.Course)
            .Include(ci => ci.CourseRole)
            .Include(ci => ci.InvitedBy)
            .FirstOrDefaultAsync(ci => ci.Token == query.Token, ct);

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
            InvalidReason = CourseTeamHelpers.GetInvalidReason(invitation)
        };

        return Result.Success(response);
    }
}
