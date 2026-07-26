using Microsoft.AspNetCore.Mvc;
using Tatakae.Api.Extensions;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Files;
using Tatakae.Application.Interfaces.Services;

namespace Tatakae.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController(IMediaAssetService media, IWebHostEnvironment environment) : ControllerBase
{
    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
        ["image/svg+xml"] = ".svg",
        ["application/pdf"] = ".pdf",
        ["application/octet-stream"] = ".bin",
        ["application/x-dst"] = ".dst",
        ["application/x-pes"] = ".pes"
    };

    private static readonly HashSet<string> AllowedArtworkExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".svg", ".pdf", ".dst", ".pes"
    };

    [HttpGet("policy")]
    public IActionResult Policy() => Ok(media.Policy);

    [HttpPost("upload")]
    [RequestSizeLimit(15_500_000)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string purpose = "EmbroideryArtwork",
        [FromForm] Guid? ownerEntityId = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0)
            return ValidationFailure("فایل خالی است.", "empty_file");

        var uploadedExtension = Path.GetExtension(file.FileName);
        if (!ExtensionByContentType.TryGetValue(file.ContentType, out var extension))
        {
            extension = uploadedExtension;
        }
        else if (string.Equals(file.ContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase)
                 && !string.IsNullOrWhiteSpace(uploadedExtension))
        {
            extension = uploadedExtension;
        }

        if (!AllowedArtworkExtensions.Contains(extension))
            return ValidationFailure("نوع فایل مجاز نیست.", "invalid_file_type");

        var storedContentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        if ((string.Equals(extension, ".dst", StringComparison.OrdinalIgnoreCase)
             || string.Equals(extension, ".pes", StringComparison.OrdinalIgnoreCase))
            && string.Equals(storedContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            storedContentType = string.Equals(extension, ".dst", StringComparison.OrdinalIgnoreCase)
                ? "application/x-dst"
                : "application/x-pes";
        }

        if ((string.Equals(extension, ".dst", StringComparison.OrdinalIgnoreCase)
             || string.Equals(extension, ".pes", StringComparison.OrdinalIgnoreCase))
            && !string.Equals(purpose, "EmbroideryArtwork", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationFailure("فایل DST/PES فقط برای طرح گلدوزی مجاز است.", "invalid_file_purpose");
        }

        if (file.Length > media.Policy.MaxSizeBytes)
            return ValidationFailure("حجم فایل بیشتر از حد مجاز است.", "file_too_large");

        var uploadsRoot = Path.Combine(
            environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
            "uploads",
            purpose.ToLowerInvariant());
        Directory.CreateDirectory(uploadsRoot);

        var safeName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(uploadsRoot, safeName);
        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var url = $"{Request.Scheme}://{Request.Host}/uploads/{purpose.ToLowerInvariant()}/{safeName}";
        var result = await media.AddStoredFileAsync(new CreateStoredFileRequest
        {
            OwnerEntityId = ownerEntityId,
            Purpose = purpose,
            FileName = file.FileName,
            ContentType = storedContentType,
            SizeBytes = file.Length,
            Url = url,
            AltText = Path.GetFileNameWithoutExtension(file.FileName)
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            System.IO.File.Delete(path);
            return result.ToActionResult(this);
        }

        return Created(result.Data!.Url, result.Data);
    }

    private IActionResult ValidationFailure(string message, string errorCode)
        => BadRequest(new ResultDto().ValidationFailed(message, errorCode));
}
