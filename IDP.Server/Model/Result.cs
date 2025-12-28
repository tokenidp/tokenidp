using System.Text.Json.Serialization;

namespace IDP.Server.Model;

public class Result<TResult>
{
    public bool IsSuccess { get; set; }
    public TResult Value { get; set; }

    [JsonConstructor]
    private Result() { }

    private Result(TResult value, bool isSuccess)
    {
        IsSuccess = isSuccess;
        Value = value;
    }

    public static Result<TResult> Success(TResult value) => new(value, true);

    public static Result<TResult> Failure(TResult error) => new(error, false);
}