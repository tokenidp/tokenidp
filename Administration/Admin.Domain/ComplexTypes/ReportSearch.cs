using System.Diagnostics.CodeAnalysis;

namespace Identity.Domain.ComplexTypes;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and private constructor for EF")]
public class ReportSearch
{
    public int Id { get; private set; }
    public string Parent { get; private set; }
    public string ReportKey { get; private set; }
    public string ReportName { get; private set; }
    public string ReportId { get; private set; }
    public string DefaultReport { get; private set; }
    public string ShowToTenant { get; private set; }
    public string Active { get; private set; }
    public string UpdateBy { get; private set; }

    private ReportSearch()
    {

    }
}
