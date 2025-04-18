using Microsoft.Extensions.Logging;

namespace Identity.Application.Common.Model;

public class LogEvent
{
    public LogLevel LogLevel { get; private set; }
    public string Message { get; private set; }
    public object Data { get; private set; }

    public LogEvent(LogLevel level, string message)
    {
        LogLevel = level;
        Message = message;
    }

    public LogEvent(LogLevel level, string message, object data)
    {
        LogLevel = level;
        Message = message;
        Data = data;
    }
}
