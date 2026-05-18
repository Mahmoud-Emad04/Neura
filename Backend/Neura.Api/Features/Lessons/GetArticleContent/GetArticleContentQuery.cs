using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.Lessons.GetArticleContent;

public sealed record GetArticleContentQuery(int LessonId, string UserId) 
    : IRequest<Result<ArticleResponse>>;
