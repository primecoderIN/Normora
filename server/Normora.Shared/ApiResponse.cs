namespace Normora.Shared;

/// <summary>
/// A standardized wrapper for all API responses to ensure a consistent JSON structure across the frontend and backend.
/// </summary>
/// <typeparam name="T">The type of the payload data returned.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the API request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// An optional message providing context for the result (e.g., error details or success confirmation).
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// The actual payload returned by the API.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Creates a successful response with the given data.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = "Success")
    {
        return new ApiResponse<T> { Success = true, Data = data, Message = message };
    }

    /// <summary>
    /// Creates a failure response with an error message.
    /// </summary>
    public static ApiResponse<T> Failure(string message)
    {
        return new ApiResponse<T> { Success = false, Message = message, Data = default };
    }
}

/// <summary>
/// A standardized wrapper for API responses that do not return any data payload (e.g., void operations).
/// </summary>
public sealed class ApiResponse
{
    /// <summary>
    /// Indicates whether the API request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// An optional message providing context for the result.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Creates a successful response without data.
    /// </summary>
    public static ApiResponse Ok(string message = "Success")
    {
        return new ApiResponse { Success = true, Message = message };
    }

    /// <summary>
    /// Creates a failure response with an error message.
    /// </summary>
    public static ApiResponse Failure(string message)
    {
        return new ApiResponse { Success = false, Message = message };
    }
}
