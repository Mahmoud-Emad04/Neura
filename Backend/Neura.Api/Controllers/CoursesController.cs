using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neura.Api.Features.Courses.CreateCourse;
using Neura.Api.Features.Courses.UpdateCourseDetails;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Course;
using Neura.Core.Contracts.Courses;
using System.Security.Claims;

namespace Neura.Api.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly ISender _sender;

    public CoursesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateCourse([FromForm] CourseRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        var command = new CreateCourseCommand(request, userId);
        var result = await _sender.Send(command, ct);

        return result.IsSuccess
            ? CreatedAtRoute("GetCourseMetadata", new { courseId = result.Value.KeyId }, result.Value)
            : result.ToProblem();
    }

    [HttpPut("{courseId}")]
    [Authorize(Policy = "CoursePermission_EditContent")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateCourseDetails(string courseId, [FromForm] CourseUpdateRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        
        var command = new UpdateCourseDetailsCommand(courseId, request, userId);
        var result = await _sender.Send(command, ct);

        return result.IsSuccess 
            ? NoContent() 
            : result.ToProblem();
    }
}
