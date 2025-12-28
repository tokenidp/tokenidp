namespace IDP.Server.Model;

public class ApiError
{
    public string CorrelationId { get; set; }
    public int UserId { get; set; }
    public string IPAddress { get; set; }
    public string SourceType { get; set; }
    public string Error { get; set; }
    public Dictionary<string, string> Errors { get; set; }
    public string Help { get; set; }

    private ApiError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Error message cannot be null or empty.", nameof(message));

        Error = message;
    }

    public static ApiError Failure(string message)
    {
        return new ApiError(message);
    }
}