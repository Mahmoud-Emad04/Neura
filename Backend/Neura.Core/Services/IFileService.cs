namespace Neura.Core.Services;

public interface IFileService
{
    Task<string> UploadImageAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default);
    Task<Guid> UploadAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default);
    Task<(FileStream? stream, string contentType, string fileName)> StreamAsync(Guid Id, string folderName, CancellationToken cancellationToken = default);
    Task<(string? path, string contentType, string fileName)> GetFilePathAsync(
         Guid id, string folderName, CancellationToken cancellationToken = default);
    void Delete(string imagePath);
}