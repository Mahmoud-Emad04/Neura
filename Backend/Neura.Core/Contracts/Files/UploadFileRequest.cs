using Microsoft.AspNetCore.Http;

namespace Neura.Core.Contracts.Files;

public record UploadFileRequest(
    IFormFile File
);
