namespace IDP.Core.Model;

public class ApiError
{
    public string CorrelationId { get; private set; }
    public int UserId { get; private set; }
    public string IPAddress { get; private set; }
    public string SourceType { get; private set; }
    public string Error { get; private set; }
    public Dictionary<string, string> Errors { get; private set; }
    public string Help { get; private set; }

    private ApiError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Error message cannot be null or empty.", nameof(message));

        Error = message;
    }

    private ApiError(string message, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Error message cannot be null or empty.", nameof(message));

        Error = message;
        CorrelationId = correlationId;
    }

    private ApiError(string message,
        string correlationId,
        int userId,
        string ipAddress,
        string sourceType)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Error message cannot be null or empty.", nameof(message));

        Error = message;
        CorrelationId = correlationId;
        UserId = userId;
        IPAddress = ipAddress;
        SourceType = sourceType;
    }

    private ApiError(Dictionary<string, string> errors,
        string correlationId,
        string help)
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors), "Errors cannot be null.");
        Help = help ?? "Refer to the documentation for further assistance.";
    }

    public static ApiError Failure(string message)
    {
        return new ApiError(message);
    }

    public static ApiError Failure(string message, string correlationId)
    {
        return new ApiError(message, correlationId);
    }

    public static ApiError Failure(string message,
        string correlationId,
        int userId,
        string ipAddress,
        string sourceType)
    {
        return new ApiError(message,
            correlationId,
            userId,
            ipAddress,
            sourceType);
    }

    public static ApiError Failure(Dictionary<string, string> errors,
        string correlationId,
        string help = "")
    {
        return new ApiError(errors, correlationId, help);
    }
}