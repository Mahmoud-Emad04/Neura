namespace Neura.Core.Contracts.Section;

public record SectionUpdateRequest(
    string Title,
    string? Description,
    int Position
);