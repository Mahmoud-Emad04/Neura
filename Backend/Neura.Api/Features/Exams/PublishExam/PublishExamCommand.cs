using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Exams.PublishExam;

public sealed record PublishExamCommand(int LessonId, string UserId) 
    : IRequest<Result>;
