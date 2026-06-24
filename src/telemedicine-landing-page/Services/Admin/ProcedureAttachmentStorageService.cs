using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Forms;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ProcedureAttachmentStorageService : IProcedureAttachmentStorageService
{
    public const long MaxFileSizeBytes = 50L * 1024 * 1024;
    private readonly string _rootPath;

    public ProcedureAttachmentStorageService(IWebHostEnvironment environment)
    {
        _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "procedure-attachments"));
    }

    public Task<StoredProcedureAttachment> SaveAsync(
        Guid versionId,
        IBrowserFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Size <= 0 || file.Size > MaxFileSizeBytes)
            throw new InvalidOperationException("Tệp phải có dung lượng từ 1 byte đến 50 MB.");

        return SaveAsync(
            versionId,
            file.Name,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            file.OpenReadStream(MaxFileSizeBytes, cancellationToken),
            cancellationToken);
    }

    public async Task<StoredProcedureAttachment> SaveAsync(
        Guid versionId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var safeName = SanitizeFileName(fileName);
        var relativePath = Path.Combine("uploads", versionId.ToString("N"), $"{Guid.NewGuid():N}-{safeName}");
        var absolutePath = GetVerifiedPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var input = content;
        await using var output = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long bytesWritten = 0;

        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            hash.AppendData(buffer, 0, read);
            bytesWritten += read;
        }

        if (bytesWritten <= 0 || bytesWritten > MaxFileSizeBytes)
            throw new InvalidOperationException("Tệp phải có dung lượng từ 1 byte đến 50 MB.");

        return new StoredProcedureAttachment(
            safeName,
            relativePath.Replace('\\', '/'),
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            bytesWritten,
            Convert.ToHexString(hash.GetHashAndReset()));
    }

    public string? ResolveAbsolutePath(string fileUri)
    {
        try
        {
            var path = GetVerifiedPath(fileUri.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(path) ? path : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private string GetVerifiedPath(string relativePath)
    {
        var absolutePath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        var rootPrefix = _rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!absolutePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đường dẫn tệp đính kèm không hợp lệ.");
        return absolutePath;
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(name) ? "attachment.bin" : name;
    }
}
