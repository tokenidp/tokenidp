namespace Services.Common.Model;

public class ApiError
{
    public string CorrelationId { get; private set; }
    public int UserId { get; private set; }
    public string IPAddress { get; private set; }
    public string SourceType { get; private set; }
    public Dictionary<string, string> Errors { get; private set; }
    public string Help { get; private set; }

    private ApiError(string correlationId, string code, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Error message cannot be null or empty.", nameof(message));

        Errors = new Dictionary<string, string> { { code, message } };
        CorrelationId = correlationId;
        Help = "Refer to the documentation for further assistance.";
    }

    private ApiError(Dictionary<string, string> errors, string correlationId, string help)
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors), "Errors cannot be null.");
        Help = help ?? "Refer to the documentation for further assistance.";
    }

    public static ApiError Failure(string message, string correlationId = "", string code = "")
    {
        return new ApiError(correlationId, code, message);
    }

    public static ApiError Failure(Dictionary<string, string> errors, string correlationId = "", string help = "")
    {
        return new ApiError(errors, correlationId, help);
    }
}
