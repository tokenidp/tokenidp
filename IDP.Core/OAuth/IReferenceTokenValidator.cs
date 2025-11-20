namespace IDP.Core.TokenServices;

internal interface IReferenceTokenValidator
{
    Task<IntrospectionResponse> ValidateReferenceToken(string referenceToken);
}
