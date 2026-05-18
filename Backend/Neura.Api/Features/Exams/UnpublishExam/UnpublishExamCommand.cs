using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Exams.UnpublishExam;

public sealed record UnpublishExamCommand(int LessonId, string UserId) 
    : IRequest<Result>;
