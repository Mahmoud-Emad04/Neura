using Microsoft.AspNetCore.Http;

namespace Neura.Core.Contracts.Course;

public record CourseUpdateRequest(
    string Title,
    string Description,
    bool IsCompleted,
    IFormFile Image,
    DateOnly Startin,
    DateOnly Endin,
    List<int> Tags);