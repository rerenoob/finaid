using System.Net;

namespace finaid.Models.Federal;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Errors { get; set; } = new();
    public HttpStatusCode StatusCode { get; set; }
    public string? RequestId { get; set; }
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }

    public static ApiResponse<T> Success(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> Error(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Errors = errors ?? new List<string>(),
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> Error(Exception exception, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorMessage = exception.Message,
            Errors = new List<string> { exception.ToString() },
            StatusCode = statusCode
        };
    }
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Success(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new ApiResponse
        {
            IsSuccess = true,
            StatusCode = statusCode
        };
    }

    public static ApiResponse Error(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.BadRequest, List<string>? errors = null)
    {
        return new ApiResponse
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Errors = errors ?? new List<string>(),
            StatusCode = statusCode
        };
    }
}