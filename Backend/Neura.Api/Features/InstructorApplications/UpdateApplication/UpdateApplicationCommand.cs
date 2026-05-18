using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.InstructorApplication;

namespace Neura.Api.Features.InstructorApplications.UpdateApplication;

public sealed record UpdateApplicationCommand(UpdateApplicationRequest Request, string UserId) 
    : IRequest<Result<ApplicationResponse>>;
