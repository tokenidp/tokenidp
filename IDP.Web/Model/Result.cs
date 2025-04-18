namespace IDP.Web.Model;

public class Result<TResult>
{
    public bool IsSuccess { get; set; }
    public TResult Value { get; set; }
    public string ErrorMessage { get; set; }
}