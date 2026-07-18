namespace MyCal.ApiService.Common.Result;

public record Result<T>(
    bool IsSuccess,
    string? ErrorMessage = null,
    string? ErrorCode = null,
    T? Data = default)
{
    public static Result<T> Success(T? data) => 
        new(Data: data, IsSuccess: true);

    public static Result<T> Fail(string? errorMessage, string? errorCode) => 
        new(
            IsSuccess: false, 
            ErrorMessage: errorMessage, 
            ErrorCode: errorCode);
}

public record Result(
    bool IsSuccess,
    string? SuccessMessage = null,
    string? ErrorMessage = null)
{
    public static Result Success(string? successMessage) => 
        new(IsSuccess: true, SuccessMessage: successMessage);

    public static Result Fail(string? errorMessage) => 
        new(IsSuccess: false, ErrorMessage: errorMessage);
}
