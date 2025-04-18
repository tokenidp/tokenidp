namespace IDP.Service.Application.TokenService;

public interface IReferenceTokenValidator
{
    Task<IntrospectionResponse> ValidateReferenceToken(string referenceToken);
}
