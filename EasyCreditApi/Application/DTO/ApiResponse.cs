namespace EasyCreditApi.Application.DTO;

// Envelope standar untuk semua response API: { status, message, data }.
public class ApiResponse<T>
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T? data, string message = "Success") =>
        new() { Status = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, T? data = default) =>
        new() { Status = false, Message = message, Data = data };
}
