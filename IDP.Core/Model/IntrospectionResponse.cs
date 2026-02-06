namespace IDP.Core.Model;

public class IntrospectionResponse
{
    public bool Active { get; set; }
    public string Sub { get; set; }
    public string UId { get; set; }
    public string? Scope { get; set; }
    public string[] Roles { get; set; }

    private IntrospectionResponse() { }

    public static IntrospectionResponse Create()
    {
        return new IntrospectionResponse()
        {
            Active = false
        };
    }

    public static IntrospectionResponse Create(int? userId,
        string clientId,
        int tenantId,
        string? scope,
        string[] roles)
    {
        return new IntrospectionResponse()
        {
            Active = true,
            Sub = userId == null ? clientId : userId.Value.ToString(),
            UId = tenantId.ToString(),
            Scope = scope,
            Roles = roles
        };
    }
}