using MediatR;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.Video.GetVideoLink;

public sealed record GetVideoLinkQuery(int LessonId, string UserId)
    : IRequest<Result<VideoLinkResponse>>;
