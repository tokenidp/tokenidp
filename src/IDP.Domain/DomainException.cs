namespace IDP.Domain;

public class DomainException : Exception
{
    public string Error { get; }

    public DomainException(string message)
        : base(message)
    {
        Error = message;
    }
}