using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure;

public class AppLogger<T> : IAppLogger<T> where T : class
{
    /// <summary>
    /// Trace = 0, Debug = 1, Information = 2, Warning = 3, Error = 4, Critical = 5, and None = 6.
    /// When a LogLevel is specified, logging is enabled for messages at the specified level and higher. 
    /// In the preceding JSON, the Default category is logged for Information and higher. For example, Information, 
    /// Warning, Error, and Critical messages are logged. If no LogLevel is specified, logging defaults to the 
    /// Information level
    /// </summary>

    private readonly ILogger<AppLogger<T>> _logger;
    private readonly JsonHelper _jsonHelper;

    public AppLogger(ILogger<AppLogger<T>> logger,
        JsonHelper jsonHelper)
    {
        _logger = logger;
        _jsonHelper = jsonHelper;
    }

    public void LogDebug(string message, params object[] args)
    {
        _logger.LogDebug(message, args);
    }

    public void LogInfo(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    public void LogTrace<TData>(TData data, string message) where TData : class
    {
        if (data == null)
            return;

        var json = _jsonHelper.SerializeFormattedObject(data);

        _logger.LogTrace(message, json);
    }

    public void LogTrace(string message, params object[] args)
    {
        _logger.LogTrace(message, args);
    }

    public void LogWarning<TData>(TData data, string message) where TData : class
    {
        if (data == null)
            return;

        var json = _jsonHelper.SerializeFormattedObject(data);

        _logger.LogWarning(message, json);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    public void LogError<TData>(TData data, string message) where TData : class
    {
        if (data == null)
            return;

        var json = _jsonHelper.SerializeFormattedObject(data);

        _logger.LogError(message, json);
    }

    public void LogError(string message, params object[] args)
    {
        _logger.LogError(message, args);
    }

    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.LogError(exception, message, args);
    }

    public void LogFatal(Exception exception, string message, params object[] args)
    {
        _logger.LogCritical(exception, message, args);
    }
}
