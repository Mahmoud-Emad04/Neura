using MediatR;

namespace Neura.Api.Features.Payment.HandleStripeWebhook;

internal sealed class HandleStripeWebhookHandler(IStripeService stripeService)
    : IRequestHandler<HandleStripeWebhookCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        HandleStripeWebhookCommand request, CancellationToken ct)
    {
        return await stripeService.HandleWebhookAsync(request.Json, request.StripeSignature, ct);
    }
}
