using Neura.Core.Contracts.Course;
using HashidsNet;

namespace Neura.Api.Mapping;

public class MappingConfiguration : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        var hashids = new Hashids("Course", 8);

        config.NewConfig<Course, CourseResponse>()
            .Map(dest => dest.KeyId, src => hashids.Encode(src.Id))
            .Map(dest => dest.Topics, src => src.Topics.Adapt<List<TopicResponse>>());
            //.Map(dest => dest.Tags, src => src.Topics.Adapt<List<TagResponse>>());

        config.NewConfig<CourseRequest, Course>()
            .Ignore(src => src.Tags);
    }
}