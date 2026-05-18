using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.InstructorApplication;

namespace Neura.Api.Features.InstructorApplications.GetMyApplicationStatus;

public sealed record GetMyApplicationStatusQuery(string UserId) 
    : IRequest<Result<MyApplicationStatusResponse>>;
