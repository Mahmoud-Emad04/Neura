using MediatR;
using Neura.Core.InstructorApplication;

namespace Neura.Api.Features.InstructorApplications.RejectApplication;

public sealed record RejectApplicationCommand(int Id, ReviewApplicationRequest Request, string ReviewerId)
    : IRequest<Result<ApplicationResponse>>;
