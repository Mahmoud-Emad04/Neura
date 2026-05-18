using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.InstructorApplication;

namespace Neura.Api.Features.InstructorApplications.GetApplicationById;

public sealed record GetApplicationByIdQuery(int Id) 
    : IRequest<Result<ApplicationResponse>>;
