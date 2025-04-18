using System.Text.Json.Serialization;

namespace ClientWebApp.Model;

public class Result<TResult>
{
    public bool IsSuccess { get; set; }
    [JsonPropertyName("value")]
    public TResult Value { get; set; }
    public string ErrorMessage { get; set; }
}
