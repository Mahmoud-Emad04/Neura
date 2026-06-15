namespace Neura.Core.Enums;

public enum VideoProcessingStatus : byte
{
    None = 0,        // No video or not yet sent for processing
    Processing = 1,  // Sent to external system, awaiting callback
    Completed = 2,   // Transcription received and saved
    Failed = 3       // External processing failed
}
