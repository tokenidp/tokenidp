namespace TokenIDP.Domain.AggregateRoots.Clients;

public sealed record ClientApiScopeAssignment(string ScopeName, string ApiResourceName);
