using MediatR;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.UpdateLessonPrivacy;

public sealed record UpdateLessonPrivacyCommand(int LessonId, UpdateLessonPrivacyRequest Request, string UserId)
    : IRequest<Result>;
