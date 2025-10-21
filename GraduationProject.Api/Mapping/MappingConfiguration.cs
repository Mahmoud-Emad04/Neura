using GraduationProject.Core.Contracts.Course;
using Microsoft.AspNetCore.DataProtection;
using HashidsNet;

namespace GraduationProject.Api.Mapping;

public class MappingConfiguration() : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        var hashids = new Hashids("Course", 8);

        config.NewConfig<Course, CourseResponse>()
            .Map(dest => dest.KeyId, src => hashids.Encode(src.Id))
            .Map(dest => dest.Topics, src => src.Topics.Adapt<List<TopicResponse>>());
    }
}