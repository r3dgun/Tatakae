using Microsoft.AspNetCore.Components.Forms;
using Tatakae.Application.Contracts.Common;
using Tatakae.Application.Contracts.Files;
using Tatakae.Web.ApiClients.Abstractions;
using Tatakae.Web.ApiClients.Results;

namespace Tatakae.Web.ApiClients.Http;

public sealed class FileUploadApiClient(HttpClient http, IApiResultReader reader) : IFileUploadApiClient
{
    public async Task<ResultDto<UploadPolicyDto>> PolicyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http.GetAsync("api/files/policy", cancellationToken);
            return await reader.ReadAsync<UploadPolicyDto>(response, "دریافت قوانین آپلود ناموفق بود.", cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ResultDto<UploadPolicyDto>().Failed(
                "زمان انتظار برای دریافت قوانین آپلود به پایان رسید.",
                ResultStatus.Failure,
                "http_timeout");
        }
        catch (HttpRequestException)
        {
            return new ResultDto<UploadPolicyDto>().Failed(
                "ارتباط با سرویس فایل برقرار نشد.",
                ResultStatus.Failure,
                "http_connection_failed");
        }
        catch (Exception)
        {
            return new ResultDto<UploadPolicyDto>().Failed(
                "دریافت قوانین آپلود ناموفق بود.",
                ResultStatus.Failure,
                "file_policy_unexpected_error");
        }
    }

    public async Task<ResultDto<FileUploadDto>> UploadAsync(
        IBrowserFile file,
        string purpose = "EmbroideryArtwork",
        CancellationToken cancellationToken = default)
    {
        if (file is null)
            return new ResultDto<FileUploadDto>().ValidationFailed("فایل برای آپلود انتخاب نشده است.", "file_required");

        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(file.OpenReadStream(15_000_000, cancellationToken));
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(streamContent, "file", file.Name);
            content.Add(new StringContent(purpose), "purpose");

            using var response = await http.PostAsync("api/files/upload", content, cancellationToken);
            return await reader.ReadAsync<FileUploadDto>(response, "آپلود فایل ناموفق بود.", cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ResultDto<FileUploadDto>().Failed(
                "زمان انتظار برای آپلود فایل به پایان رسید.",
                ResultStatus.Failure,
                "http_timeout");
        }
        catch (ArgumentOutOfRangeException)
        {
            return new ResultDto<FileUploadDto>().ValidationFailed(
                "حجم فایل بیشتر از حد مجاز ۱۵ مگابایت است.",
                "file_too_large");
        }
        catch (HttpRequestException)
        {
            return new ResultDto<FileUploadDto>().Failed(
                "ارتباط با سرویس فایل برقرار نشد.",
                ResultStatus.Failure,
                "http_connection_failed");
        }
        catch (Exception)
        {
            return new ResultDto<FileUploadDto>().Failed(
                "آپلود فایل ناموفق بود.",
                ResultStatus.Failure,
                "file_upload_unexpected_error");
        }
    }
}
