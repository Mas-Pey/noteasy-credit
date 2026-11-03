using System;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using EasyCreditFrontend.Models;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace EasyCreditFrontend.Services;
public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _apiKey;

    public ApiClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _apiKey = configuration["ApiSettings:ApiKey"] ?? string.Empty;
    }

    public Task<ApiResponse<T>?> GetAsync<T>(string url)
        => SendAsync<T>(HttpMethod.Get, url, null);

    public Task<ApiResponse<T>?> PostAsync<T>(string url, object? body = null)
        => SendAsync<T>(HttpMethod.Post, url, body);

    public Task<ApiResponse<T>?> PutAsync<T>(string url, object? body = null)
        => SendAsync<T>(HttpMethod.Put, url, body);

    public Task<ApiResponse<object>?> DeleteAsync(string url)
        => SendAsync<object>(HttpMethod.Delete, url, null);

    private async Task<ApiResponse<T>?> SendAsync<T>(HttpMethod method, string url, object? body)
    {
        using var request = new HttpRequestMessage(method, url);

        // API key middleware di EasyCreditApi hanya aktif untuk /api.
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            request.Headers.TryAddWithoutValidation("X-Api-Key", _apiKey);
        }

        // Token JWT disimpan di session server-side, bukan localStorage browser.
        var token = _httpContextAccessor.HttpContext?.Session.GetString(SessionKeys.Token);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        try
        {
            using var response = await _httpClient.SendAsync(request);

            if (response.Content.Headers.ContentLength is null or 0)
            {
                return new ApiResponse<T>
                {
                    Status = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode
                        ? "Berhasil"
                        : $"Request gagal ({(int)response.StatusCode})"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
            return result ?? new ApiResponse<T>
            {
                Status = false,
                Message = "Response tidak valid"
            };
        }
        catch (HttpRequestException ex)
        {
            return new ApiResponse<T>
            {
                Status = false,
                Message = $"Gagal menghubungi EasyCredit API: {ex.Message}"
            };
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse<T>
            {
                Status = false,
                Message = "Request ke EasyCredit API timeout."
            };
        }
        catch (JsonException)
        {
            return new ApiResponse<T>
            {
                Status = false,
                Message = "Format response dari EasyCredit API tidak dikenali."
            };
        }
    }
}
