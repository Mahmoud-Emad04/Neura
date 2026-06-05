using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.CourseTeam;

namespace Neura.Api.Features.CourseTeam.TransferOwnership;

public sealed record TransferOwnershipCommand(int CourseId, TransferOwnershipRequest Request, string RequesterId) 
    : IRequest<Result>;
