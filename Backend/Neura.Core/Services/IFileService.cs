using Microsoft.AspNetCore.Http;

namespace Neura.Core.Services;

public interface IFileService
{
    Task<string> UploadImageAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default);
    void Delete(string imagePath);
}
