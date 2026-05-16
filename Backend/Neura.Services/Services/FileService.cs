using Microsoft.AspNetCore.Hosting;

namespace Neura.Services.Services;

public class FileService(IWebHostEnvironment webHostEnvironment, ApplicationDbContext context) : IFileService
{
    private readonly ApplicationDbContext _context = context;
    private readonly string _filesPath = $"{webHostEnvironment.WebRootPath}/Files";
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

        return $"Images/{folderName}/{uniqueName}";
    }

    public async Task<Guid> UploadAsync(IFormFile file, string folderName,
        CancellationToken cancellationToken = default)
    {
        var uploadedFile = await StreamFile(file, folderName, cancellationToken);

        await _context.AddAsync(uploadedFile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return uploadedFile.Id;
    }

    public async Task<(FileStream? stream, string contentType, string fileName)> StreamAsync(Guid Id, string folderName,
        CancellationToken cancellationToken = default)
    {
        var file = await _context.UploadedFiles.FindAsync(Id, cancellationToken);

        if (file is null)
            return (null, string.Empty, string.Empty);

        var path = Path.Combine(_filesPath, folderName, file.StoredFileName);

        var fileStream = File.OpenRead(path);

        return (fileStream, file.ContentType, file.FileName);
    }

    // Add this method to your interface and class
    public async Task<(string? path, string contentType, string fileName)> GetFilePathAsync(
        Guid id, string folderName, CancellationToken cancellationToken = default)
    {
        var file = await _context.UploadedFiles.FindAsync([id], cancellationToken);

        if (file is null) return (null, string.Empty, string.Empty);

        // Combine paths securely
        var physicalPath = Path.Combine(_filesPath, folderName, file.StoredFileName);

        if (!File.Exists(physicalPath)) return (null, string.Empty, string.Empty);

        return (physicalPath, file.ContentType, file.FileName);
    }

    public void Delete(string imagePath)
    {
        var oldImagePath = $"{_webHostEnvironment.WebRootPath}{imagePath}";

        if (File.Exists(oldImagePath))
            File.Delete(oldImagePath);
    }

    private async Task<UploadedFile> StreamFile(IFormFile file, string folderName, CancellationToken cancellationToken)
    {
        var fileExtension = Path.GetExtension(file.FileName);
        var randomFileName = Path.GetRandomFileName();
        var storedFileName = Path.ChangeExtension(randomFileName, fileExtension);

        var uploadedFile = new UploadedFile
        {
            FileName = file.FileName,
            StoredFileName = storedFileName,
            ContentType = file.ContentType,
            FileExtension = fileExtension
        };

        var path = Path.Combine(_filesPath, folderName, storedFileName);

        using var stream = File.Create(path);
        await file.CopyToAsync(stream, cancellationToken);

        return uploadedFile;
    }
}