using MediatR;
using Neura.Core.InstructorApplication;

namespace Neura.Api.Features.InstructorApplications.ApproveApplication;

public sealed record ApproveApplicationCommand(int Id, string ReviewerId)
    : IRequest<Result<ApplicationResponse>>;
