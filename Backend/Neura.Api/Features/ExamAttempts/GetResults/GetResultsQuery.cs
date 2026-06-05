using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Features.ExamAttempts.GetResults;

public sealed record GetResultsQuery(int AttemptId, string UserId) 
    : IRequest<Result<AttemptResultResponse>>;
