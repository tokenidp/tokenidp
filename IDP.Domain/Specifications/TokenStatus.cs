namespace IDP.Domain.Specifications;

public enum TokenStatus
{
    Active = 0,
    Expired = 1,
    Revoked = 2,
    Compromised = 3,
    Suspended = 4
}