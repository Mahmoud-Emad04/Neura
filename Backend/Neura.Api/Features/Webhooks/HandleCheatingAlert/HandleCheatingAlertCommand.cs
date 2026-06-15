using MediatR;
using Neura.Core.Contracts.Webhook;

namespace Neura.Api.Features.Webhooks.HandleCheatingAlert;

public sealed record HandleCheatingAlertCommand(CheatingAlertRequest Request)
    : IRequest<Result>;
