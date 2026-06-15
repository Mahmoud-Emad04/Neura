using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Webhook;

namespace Neura.Api.Features.Webhooks.HandleVideoTranscription;

public sealed record HandleVideoTranscriptionCommand(VideoTranscriptionRequest Request)
    : IRequest<Result>;
