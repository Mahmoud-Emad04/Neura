namespace Neura.Core.Contracts.Lessons;

public record GetSignedVideoUploadRequest(
    int LessonId
);

public record SignedVideoUploadResponse(
    string CloudName,
    string UploadUrl,
    string ApiKey,
    string Signature,
    long Timestamp,
    string Folder,
    long MaxFileSize,
    string PublicId,
    string AllowedFormats
);

public record FinalizeVideoUploadRequest(
    string PublicId,
    string VideoUrl,
    double DurationSeconds
    );


public record FinalizeVideoUploadResponse(
    int LessonId,
    string PublicId,
    string VideoUrl,
    TimeSpan Duration,
    string Message = "Video successfully linked to lesson."
);
public record DeleteLessonVideoRequest(
    int LessonId
);

public record VideoLinkResponse(
    int LessonId,
    string VideoUrl,
    double DurationSeconds,
    bool IsVideoPrivate
);
