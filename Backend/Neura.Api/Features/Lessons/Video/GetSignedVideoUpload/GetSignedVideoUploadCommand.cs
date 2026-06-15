using MediatR;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.Video.GetSignedVideoUpload;

public sealed record GetSignedVideoUploadCommand(int LessonId, string UserId)
    : IRequest<Result<SignedVideoUploadResponse>>;
