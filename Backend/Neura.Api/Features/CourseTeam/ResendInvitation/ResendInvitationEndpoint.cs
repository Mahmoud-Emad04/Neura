// ---------------------------------------------------------------------------
//  Minimal API endpoint — COMMENTED OUT
//  Routing is now handled by the Controller (CQRS via MediatR).
//  Keep this file for reference; delete when the controller is stable.
// ---------------------------------------------------------------------------

//using MediatR;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Routing;
//using Neura.Api.Infrastructure;
//using Neura.Core.Authorization.Attributes;
//using Neura.Core.Enums;
//using System.Security.Claims;

//namespace Neura.Api.Features.CourseTeam.ResendInvitation;

//public sealed class ResendInvitationEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapPost("api/courses/{courseId:int}/team/invitations/{invitationId:int}/resend", async (
//            int courseId,
//            int invitationId,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var requesterId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var command = new ResendInvitationCommand(courseId, invitationId, requesterId);
//            var result = await sender.Send(command, ct);

//            return result.IsSuccess 
//                ? Results.Ok(result.Value) 
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithMetadata(new HasCoursePermissionAttribute(CoursePermission.ManageTeam))
//        .WithTags("CourseTeam")
//        .WithName("ResendInvitation");
//    }
//}
