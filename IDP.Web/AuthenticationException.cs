namespace IDP.Web;

public class AuthenticationException : Exception
{
    public int StatusCode { get; }

    public AuthenticationException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public AuthenticationException(string message, Exception innerException)
        : base(message, innerException) { }
}

