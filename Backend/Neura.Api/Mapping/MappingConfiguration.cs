using HashidsNet;
using Neura.Core.Contracts.Announcement;
using Neura.Core.Contracts.Exam;
using Neura.Core.Contracts.Instructor;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Contracts.Question;
using Neura.Core.Contracts.Section;
using Neura.Core.Enums;

namespace Neura.Api.Mapping;

public class MappingConfiguration : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        var hashids = new Hashids("f1nd1ngn3m0", 11);

        config.NewConfig<Course, CourseResponse>()
            .Map(dest => dest.KeyId, src => hashids.Encode(src.Id))
            .Map(dest => dest.Sections, src => src.Sections.Adapt<List<SectionResponse>>());

        config.NewConfig<Course, CourseMetadataResponse>()
            .Map(dest => dest.KeyId, src => hashids.Encode(src.Id))
            .Map(dest => dest.Prerequisites, src => src.Prerequisites.Select(p => p.Requirement))
            .Map(dest => dest.LearningOutcomes, src => src.LearningOutcomes.Select(p => p.Outcome))
            .Map(dest => dest.Tags, src => src.Tags.Select(t => new CourseMetadataTagResponse(t.Name, t.Id)).ToList());

        config.NewConfig<Section, SectionResponse>()
            .Map(dest => dest.TotalMinutes, src => (int)src.Lessons.Sum(l => l.Duration.TotalMinutes))
            .Map(dest => dest.LessonsCount, src => src.Lessons.Count);

        config.NewConfig<Lesson, LessonResponse>()
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.Exam, src => src.Type == LessonType.Quiz ? src.Exam : null)
            .Map(dest => dest.IsLocked, src => !src.IsPreview);

        config.NewConfig<Exam, ExamPreviewInfo>()
            .Map(dest => dest.TotalQuestions, src => src.Questions.Count);

        config.NewConfig<CourseBookmark, CourseSummaryResponse>()
            .Map(dest => dest, src => src.Course)
            .Map(dest => dest.KeyId, src => hashids.Encode(src.CourseId))
            .Map(dest => dest.IsBookmarked, src => true);

        config.NewConfig<Course, CourseSummaryResponse>()
            .Map(dest => dest.KeyId, src => hashids.Encode(src.Id))
            .Map(dest => dest.StatusName, src => src.Status)
            .Map(dest => dest.InstructorName, src => src.DisplayInstructorName);

        config.NewConfig<CourseUpdateRequest, Course>()
            .Map(dest => dest.Prerequisites,
                src => src.Prerequisites.Select(p => new CoursePrerequisite { Requirement = p }).ToList())
            .Map(dest => dest.LearningOutcomes,
                src => src.LearningOutcomes.Select(p => new CourseLearningOutcome { Outcome = p }).ToList())
            .Ignore(src => src.Tags);
        config.NewConfig<CourseRequest, Course>()
            .Map(dest => dest.Prerequisites,
                src => src.Prerequisites.Select(p => new CoursePrerequisite { Requirement = p }).ToList())
            .Map(dest => dest.LearningOutcomes,
                src => src.LearningOutcomes.Select(p => new CourseLearningOutcome { Outcome = p }).ToList())
            .Ignore(src => src.Tags);

        config.NewConfig<ApplicationUser, InstructorSummaryResponse>();

        config.NewConfig<Post, PostResponse>();
        config.NewConfig<PostComment, PostCommentResponse>();

        config.NewConfig<Exam, ExamResponse>()
          .Map(dest => dest.TotalQuestions, src => src.Questions.Count)
          .Map(dest => dest.TotalPoints, src => src.Questions.Sum(q => q.Points))
          .Map(dest => dest.TotalAttempts, src => src.Attempts.Count);
        // CreatedOn, UpdatedOn map automatically from AuditableEntity

        // ── Exam → ExamDetailResponse ──
        config.NewConfig<Exam, ExamDetailResponse>()
            .Map(dest => dest.TotalQuestions, src => src.Questions.Count)
            .Map(dest => dest.TotalPoints, src => src.Questions.Sum(q => q.Points))
            .Map(dest => dest.TotalAttempts, src => src.Attempts.Count)
            .Ignore(dest => dest.Questions);

        // ── Question → QuestionResponse ──
        config.NewConfig<Question, QuestionResponse>()
            .Map(dest => dest.Options, src => src.AnswerOptions.OrderBy(a => a.Order))
            .Ignore(dest => dest.HasAttempts);

        // ── AnswerOption → AnswerOptionResponse ──
        config.NewConfig<AnswerOption, AnswerOptionResponse>();

        // ── CreateExamRequest → Exam ──
        config.NewConfig<CreateExamRequest, Exam>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.IsPublished)
            .Ignore(dest => dest.AreGradesPublished)
            .Ignore(dest => dest.ShowCorrectAnswersAfterSubmit)
            .Ignore(dest => dest.CreatedOn)
            .Ignore(dest => dest.UpdatedOn)
            .Ignore(dest => dest.CreatedById)
            .Ignore(dest => dest.UpdatedById)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.UpdatedBy)
            .Ignore(dest => dest.IsDeleted)
            .Ignore(dest => dest.Lesson)
            .Ignore(dest => dest.Questions)
            .Ignore(dest => dest.Attempts);
    }
}