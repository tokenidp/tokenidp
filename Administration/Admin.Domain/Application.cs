using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public class Application : BaseEntity
{
    public string AppName { get; private set; }

    private Application() { }
}
