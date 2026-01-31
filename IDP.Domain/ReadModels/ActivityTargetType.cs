namespace IDP.Domain.ReadModels;


public enum ActivityTargetType
{
    User = 1,
    Client = 2,
    Token = 3,
    Role = 4,
    Permission = 5,
    Tenant = 6,
    SecurityPolicy = 7,
    Job = 8
}