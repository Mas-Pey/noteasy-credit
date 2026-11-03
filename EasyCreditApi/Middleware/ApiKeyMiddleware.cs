using EasyCreditApi.Application.DTO;

namespace EasyCreditApi.Middleware;

public class ApiKeyMiddleware
{
    private const string HeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly string _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _apiKey = config["ApiKey"] ?? throw new InvalidOperationException("ApiKey is not configured");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) || provided != _apiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Invalid or missing API key"));
            return;   // short-circuit: the controller never runs
        }

        await _next(context);
    }
}
