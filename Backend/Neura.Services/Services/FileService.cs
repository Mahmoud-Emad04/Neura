using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Neura.Services.Services;

public class FileService(IWebHostEnvironment webHostEnvironment, ApplicationDbContext context) : IFileService
{
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
    private readonly string _imagesPath = $"{webHostEnvironment.WebRootPath}/Images";
    private readonly ApplicationDbContext _context = context;

    public async Task<string> UploadImageAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default)
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
