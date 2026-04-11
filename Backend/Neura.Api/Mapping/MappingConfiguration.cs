using HashidsNet;
using Neura.Core.Contracts.Announcement;
using Neura.Core.Contracts.Instructor;
using Neura.Core.Contracts.Section;

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
            .Map(dest => dest.LearningOutcomes, src => src.LearningOutcomes.Select(p => p.Outcome));


        config.NewConfig<Section, SectionResponse>()
            .Map(dest => dest.TotalMinutes, src => (int)src.Lessons.Sum(l => l.Duration.TotalMinutes));

        config.NewConfig<CourseBookmark, CourseSummaryResponse>()
            .Map(dest => dest, src => src.Course)
            .Map(dest => dest.KeyId, src => hashids.Encode(src.CourseId))
            .Map(dest => dest.IsBookmarked, src => true);

        config.NewConfig<Course, CourseSummaryResponse>()
            .Map(dest => dest.KeyId, src => hashids.Encode(src.Id));

        //config.NewConfig<CourseMetadataResponse, Course>()
        //    .Map(dest => dest.Prerequisites,
        //        src => src.Prerequisites!.Select(p => new CoursePrerequisite { Requirement = p }).ToList())
        //    .Map(dest => dest.LearningOutcomes,
        //        src => src.LearningOutcomes!.Select(p => new CourseLearningOutcome { Outcome = p }).ToList())
        //    .Ignore(src => src.Tags);

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
    }
}