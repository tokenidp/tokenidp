using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model")]
public class StateLookup : BaseEntity, IAggregateRoot
{
    public string State { get; private set; }
    public string Code { get; private set; }
    public string Country { get; private set; }
}
