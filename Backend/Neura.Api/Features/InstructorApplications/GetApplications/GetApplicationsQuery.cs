using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Enums;
using Neura.Core.InstructorApplication;

namespace Neura.Api.Features.InstructorApplications.GetApplications;

public sealed record GetApplicationsQuery(ApplicationStatus? Status, int PageNumber, int PageSize) 
    : IRequest<Result<PaginatedList<ApplicationListResponse>>>;
