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
//using Neura.Core.Abstractions;
//using System.Security.Claims;

//namespace Neura.Api.Features.Community.GetMessageHistory;

//public sealed class GetMessageHistoryEndpoint : IEndpoint
//{
//    public void MapEndpoint(IEndpointRouteBuilder app)
//    {
//        app.MapGet("api/community/channels/{channelId:int}/messages", async (
//            int channelId,
//            [AsParameters] GetMessageHistoryRequest request,
//            ClaimsPrincipal user,
//            ISender sender,
//            CancellationToken ct) =>
//        {
//            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;

//            var query = new GetMessageHistoryQuery(
//                channelId, userId, request.Before, request.PageSize);

//            var result = await sender.Send(query, ct);

//            return result.IsSuccess 
//                ? Results.Ok(result.Value.ToViewModel()) 
//                : result.ToProblemMinimal();
//        })
//        .RequireAuthorization()
//        .WithTags("Community")
//        .WithName("GetMessageHistory");
//    }
//}

//public static class GetMessageHistoryMapper
//{
//    public static PagedMessagesResponseViewModel ToViewModel(this Neura.Core.Contracts.Community.PagedMessagesDto dto)
//    {
//        var messages = new MessageResponseViewModel[dto.Messages.Count];

//        for (int i = 0; i < dto.Messages.Count; i++)
//        {
//            var m = dto.Messages[i];
//            messages[i] = new MessageResponseViewModel(
//                m.Id, m.SenderName, m.SenderAvatarUrl, m.Content, m.SentAt, m.IsDeleted);
//        }

//        return new PagedMessagesResponseViewModel(messages, dto.NextCursor, dto.HasMore);
//    }
//}
