using GraduationProject.Core.Abstractions;
using GraduationProject.Core.Contracts.Course;
using GraduationProject.Core.Entities;
using GraduationProject.Core.Errors;
using GraduationProject.Core.Service;
using GraduationProject.Repository.Persistence;
using HashidsNet;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace GraduationProject.Services.Services;

public class CourseService(ApplicationDbContext context) : ICourseService
{
    private readonly ApplicationDbContext _context = context;
    private readonly Hashids _hashids = new("Course", 8);

    public async Task<Result<IEnumerable<CourseResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var courses = await _context.Courses
            .Include(c => c.Topics)
            .ToListAsync(cancellationToken);

        var response = courses.Adapt<IEnumerable<CourseResponse>>();
        return Result.Success(response);
    }

    public async Task<Result<CourseResponse>> GetByIdAsync(string keyId, CancellationToken cancellationToken = default)
    {
        int[] numbers = _hashids.Decode(keyId);
        
        if (numbers.Length == 0)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        int courseId = numbers[0];

        var course = await _context.Courses
            .Include(c => c.Topics)
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var response = course.Adapt<CourseResponse>();
        return Result.Success(response);
    }

    public async Task<Result> CreateAsync(CourseRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        var course = request.Adapt<Course>();
        course.CreatedById = userId;
        
        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
    
    
}