using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.UpdateArticleContent;

public sealed record UpdateArticleContentCommand(int LessonId, UpdateArticleRequest Request, string UserId) 
    : IRequest<Result>;
