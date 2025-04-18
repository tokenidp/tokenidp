namespace Identity.Application.Common.Interfaces;

public interface IEmailNotification
{
    /// <summary>
    /// Send email through smtp when error occured
    /// </summary>
    /// <param name="message">message</param>
    /// <param name="ex">error</param>
    /// <returns>void</returns>
    Task SendEmail(string message, Exception ex);
}
