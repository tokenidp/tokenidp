namespace TokenIDP.Infrastructure;

internal class NullAppLogger<T> : IAppLogger<T> where T : class
{
    public static readonly NullAppLogger<T> Instance = new();

    public void LogDebug(string message, params object[] args)
    {

    }

    public void LogError<TData>(string message, TData data) where TData : class
    {

    }

    public void LogError(string message, params object[] args)
    {

    }

    public void LogError(Exception exception, string message, params object[] args)
    {

    }

    public void LogFatal(Exception exception, string message, params object[] args)
    {

    }

    public void LogInfo(string message, params object[] args)
    {

    }

    public void LogTrace<TData>(string message, TData data) where TData : class
    {

    }

    public void LogTrace(string message, params object[] args)
    {

    }

    public void LogWarning<TData>(string message, TData data) where TData : class
    {

    }

    public void LogWarning(string message, params object[] args)
    {

    }
}

