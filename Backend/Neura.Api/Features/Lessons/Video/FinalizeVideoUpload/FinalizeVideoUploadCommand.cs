using MediatR;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.Video.FinalizeVideoUpload;

public sealed record FinalizeVideoUploadCommand(int LessonId, FinalizeVideoUploadRequest Request, string UserId)
    : IRequest<Result<FinalizeVideoUploadResponse>>;
