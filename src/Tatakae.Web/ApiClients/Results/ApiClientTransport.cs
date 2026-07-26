using System.Net.Http.Json;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Web.ApiClients.Results;

public sealed class ApiClientTransport(HttpClient http, IApiResultReader reader) : IApiClientTransport
{
    public async Task<ResultDto<T>> GetResultAsync<T>(
        string url,
        string fallbackMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http.GetAsync(url, cancellationToken);
            return await reader.ReadAsync<T>(response, fallbackMessage, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ResultDto<T>().Failed(
                "زمان انتظار برای دریافت پاسخ سرویس به پایان رسید.",
                ResultStatus.Failure,
                "http_timeout");
        }
        catch (HttpRequestException)
        {
            return new ResultDto<T>().Failed(
                "ارتباط با سرویس برقرار نشد. لطفاً اتصال شبکه را بررسی کنید.",
                ResultStatus.Failure,
                "http_connection_failed");
        }
        catch (Exception)
        {
            return new ResultDto<T>().Failed(
                fallbackMessage,
                ResultStatus.Failure,
                "web_client_unexpected_error");
        }
    }

    public async Task<ResultDto<T>> SendResultAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        string fallbackMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null)
                request.Content = JsonContent.Create(body);

            using var response = await http.SendAsync(request, cancellationToken);
            return await reader.ReadAsync<T>(response, fallbackMessage, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ResultDto<T>().Failed(
                "زمان انتظار برای دریافت پاسخ سرویس به پایان رسید.",
                ResultStatus.Failure,
                "http_timeout");
        }
        catch (HttpRequestException)
        {
            return new ResultDto<T>().Failed(
                "ارتباط با سرویس برقرار نشد. لطفاً اتصال شبکه را بررسی کنید.",
                ResultStatus.Failure,
                "http_connection_failed");
        }
        catch (Exception)
        {
            return new ResultDto<T>().Failed(
                fallbackMessage,
                ResultStatus.Failure,
                "web_client_unexpected_error");
        }
    }

    public async Task<ResultDto> SendResultAsync(
        HttpMethod method,
        string url,
        object? body,
        string fallbackMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null)
                request.Content = JsonContent.Create(body);

            using var response = await http.SendAsync(request, cancellationToken);
            return await reader.ReadAsync(response, fallbackMessage, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ResultDto().Failed(
                "زمان انتظار برای دریافت پاسخ سرویس به پایان رسید.",
                ResultStatus.Failure,
                "http_timeout");
        }
        catch (HttpRequestException)
        {
            return new ResultDto().Failed(
                "ارتباط با سرویس برقرار نشد. لطفاً اتصال شبکه را بررسی کنید.",
                ResultStatus.Failure,
                "http_connection_failed");
        }
        catch (Exception)
        {
            return new ResultDto().Failed(
                fallbackMessage,
                ResultStatus.Failure,
                "web_client_unexpected_error");
        }
    }
}
