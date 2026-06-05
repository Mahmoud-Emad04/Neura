using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Payment.HandleStripeWebhook;

public sealed record HandleStripeWebhookCommand(string Json, string StripeSignature)
    : IRequest<Result<string>>;
