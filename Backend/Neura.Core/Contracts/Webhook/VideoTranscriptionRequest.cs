namespace Neura.Core.Contracts.Webhook;

public sealed record VideoTranscriptionRequest(
    int LessonId,
    string VideoText
);
