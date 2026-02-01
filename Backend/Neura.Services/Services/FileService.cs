using Microsoft.AspNetCore.Hosting;

namespace Neura.Services.Services;

public class FileService(IWebHostEnvironment webHostEnvironment, ApplicationDbContext context) : IFileService
{
    private readonly ApplicationDbContext _context = context;
    private readonly string _imagesPath = $"{webHostEnvironment.WebRootPath}/Images";
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

    public async Task<string> UploadImageAsync(IFormFile file, string folderName,
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(file.FileName);
        var uniqueName = $"{Guid.NewGuid()}{ext}";

        var path = Path.Combine(_imagesPath, folderName, uniqueName);
        using var stream = File.Create(path);
        await file.CopyToAsync(stream, cancellationToken);

        return Path.Combine("Images", folderName, uniqueName);
    }

    public void Delete(string imagePath)
    {
        var oldImagePath = $"{_webHostEnvironment.WebRootPath}{imagePath}";

        if (File.Exists(oldImagePath))
            File.Delete(oldImagePath);
    }
}