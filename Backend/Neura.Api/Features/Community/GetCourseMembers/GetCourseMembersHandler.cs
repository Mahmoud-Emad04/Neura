using MediatR;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.GetCourseMembers;

internal sealed class GetCourseMembersHandler(IChatService chatService)
    : IRequestHandler<GetCourseMembersQuery, IReadOnlyList<CourseMemberDto>>
{
    public async Task<IReadOnlyList<CourseMemberDto>> Handle(
        GetCourseMembersQuery query, CancellationToken ct)
    {
        return await chatService.GetCourseMembersAsync(query.CourseId, query.UserId, ct);
    }
}
