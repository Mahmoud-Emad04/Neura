using MediatR;

namespace Neura.Api.Features.Payment.HandleStripeWebhook;

public sealed record HandleStripeWebhookCommand(string Json, string StripeSignature)
    : IRequest<Result<string>>;
