namespace TokenIDP.Core.Foundation.Exceptions;

public sealed class AuthorizationRequestException : Exception
{
    public string Error { get; }
    public string ErrorDescription { get; }
    public bool AllowRedirect { get; }

    public AuthorizationRequestException(
        string error,
        string description = "",
        bool allowRedirect = true)
        : base(description)
    {
        Error = error;
        ErrorDescription = description;
        AllowRedirect = allowRedirect;
    }
}

