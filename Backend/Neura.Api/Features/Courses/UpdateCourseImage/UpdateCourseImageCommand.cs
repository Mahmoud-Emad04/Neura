using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Files;

namespace Neura.Api.Features.Courses.UpdateCourseImage;

public sealed record UpdateCourseImageCommand(string CourseIdKey, UploadImageRequest Request, string UserId) 
    : IRequest<Result>;
