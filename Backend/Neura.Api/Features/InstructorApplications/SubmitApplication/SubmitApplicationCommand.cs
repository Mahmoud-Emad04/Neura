using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.InstructorApplication;

namespace Neura.Api.Features.InstructorApplications.SubmitApplication;

public sealed record SubmitApplicationCommand(SubmitApplicationRequest Request, string UserId) 
    : IRequest<Result<ApplicationResponse>>;
