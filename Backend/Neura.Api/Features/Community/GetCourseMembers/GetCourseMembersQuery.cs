using MediatR;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.GetCourseMembers;

public sealed record GetCourseMembersQuery(int CourseId, string UserId)
    : IRequest<IReadOnlyList<CourseMemberDto>>;
