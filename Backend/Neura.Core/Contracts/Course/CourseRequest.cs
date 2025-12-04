using Microsoft.AspNetCore.Http;

namespace Neura.Core.Contracts.Course;

public record CourseRequest(
    string Title,
    string Description,
    DateOnly Startin,
    DateOnly Endin,
    List<int> Tags
);