namespace TokenIDP.Core.OAuth.ExternalProviders.Model;

public sealed record ExternalAuthCallbackInput(
    ExternalProviderTypes Provider,
    string Code,
    string State
);

