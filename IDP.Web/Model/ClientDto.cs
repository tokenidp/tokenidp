using System.Text.Json.Serialization;

namespace IDP.Web.Model;

public class ClientDto
{
    public bool IsValidClient { get; set; }
    public string Scopes { get; set; }
}