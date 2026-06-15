using Neura.Core.Enums;

namespace Neura.Core.Entities;

public class Lesson : AuditableEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LessonStatus Status { get; set; } = LessonStatus.Draft;



    // Cloudinary video storage
    public string? CloudinaryVideoUrl { get; set; }
    public string? CloudinaryPublicId { get; set; }
    public bool IsVideoPrivate { get; set; } = false;

    // Video processing / transcription
    public string? VideoText { get; set; }
    public VideoProcessingStatus VideoProcessingStatus { get; set; } = VideoProcessingStatus.None;

    public string? ArticleContent { get; set; }
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;

    public int OrderIndex { get; set; }
    public bool IsPreview { get; set; } = false;
    public LessonType Type { get; set; }

    public bool IsPublished { get; set; }
    public DateTime? ScheduledDate { get; set; }

    public int SectionId { get; set; }
    public Section Section { get; set; } = default!;

    public Exam? Exam { get; set; }

    public ICollection<LessonCompletion> Completions { get; set; } = [];

    public void SetVideoTranscription(string videoText)
    {
        VideoText = videoText;
        VideoProcessingStatus = VideoProcessingStatus.Completed;
        UpdatedOn = DateTime.UtcNow;
    }

    public void MarkVideoProcessing()
    {
        VideoProcessingStatus = VideoProcessingStatus.Processing;
        UpdatedOn = DateTime.UtcNow;
    }

    public void MarkVideoProcessingFailed()
    {
        VideoProcessingStatus = VideoProcessingStatus.Failed;
        UpdatedOn = DateTime.UtcNow;
    }
}