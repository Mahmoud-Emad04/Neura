namespace Neura.Core.Contracts.Section;

public record SectionRequest(
	string Title,
	string? Description,
	int Position
);
