using HashidsNet;
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
		//.Map(dest => dest.Tags, src => src.Topics.Adapt<List<TagResponse>>());

		config.NewConfig<Section, SectionResponse>();

		config.NewConfig<CourseRequest, Course>()
			.Ignore(src => src.Tags);
	}
}