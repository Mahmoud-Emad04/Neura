using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Payment;

namespace Neura.Api.Features.Payment.CreateCheckoutSession;

public sealed record CreateCheckoutSessionCommand(string CourseId, string UserId)
    : IRequest<Result<CreateCheckoutSessionResponse>>;
