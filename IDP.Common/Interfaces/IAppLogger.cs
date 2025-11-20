namespace IDP.Common.Interfaces;

public interface IAppLogger<T> where T : class
{
    /// <summary>
    /// Log Debug message
    /// </summary>
    /// <param name="message">Debug message</param>
    /// <param name="args">we can add multiple arguments to string formatter</param>
    void LogDebug(string message, params object[] args);

    /// <summary>
    /// Log Trace message
    /// </summary>
    /// <typeparam name="TData">Trace Data</typeparam>
    /// <param name="message">Trace message</param>
    /// <param name="data">Trace Data</param>
    void LogTrace<TData>(string message, TData data) where TData : class;

    /// <summary>
    /// Log Trace message
    /// </summary>
    /// <param name="message">Trace message</param>
    /// <param name="args">we can add multiple arguments to string formatter</param>
    void LogTrace(string message, params object[] args);

    /// <summary>
    /// Log Information message
    /// </summary>
    /// <param name="message">Information message</param>
    /// <param name="args">we can add multiple arguments to string formatter</param>
    void LogInfo(string message, params object[] args);

    /// <summary>
    /// Log Warning message
    /// </summary>
    /// <typeparam name="TData">Error Data</typeparam>
    /// <param name="message">Error message</param>
    /// <param name="data">Error Data</param>
    void LogWarning<TData>(string message, TData data) where TData : class;

    /// <summary>
    /// Log Warning message
    /// </summary>
    /// <param name="message">Warning message</param>
    /// <param name="args">we can add multiple arguments to string formatter</param>
    void LogWarning(string message, params object[] args);

    /// <summary>
    /// Log Error message with data
    /// </summary>
    /// <typeparam name="TData">Error Data</typeparam>
    /// <param name="message">Error message</param>
    /// <param name="data">Error Data</param>
    void LogError<TData>(string message, TData data) where TData : class;

    /// <summary>
    /// Log Error message with exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="args">we can add multiple arguments to string formatter</param>
    void LogError(string message, params object[] args);

    /// <summary>
    /// Log Error message with exception
    /// </summary>
    /// <param name="exception">Application Exception</param>
    /// <param name="message">Error message</param>
    /// <param name="args">we can add multiple arguments to string formatter</param>
    void LogError(Exception exception, string message, params object[] args);

    /// <summary>
    /// Log Critical Error message with exception
    /// </summary>
    /// <param name="exception">Application Exception</param>
    /// <param name="message">Error message</param>
    /// <param name="args">we can add multiple arguments to string formatter</param>
    void LogFatal(Exception exception, string message, params object[] args);
}
