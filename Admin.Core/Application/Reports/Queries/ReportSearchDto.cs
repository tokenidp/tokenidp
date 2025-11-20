using Admin.Core.Application.Mappings;

namespace Identity.Application.Reports.Queries;

public class ReportSearchDto : IMapFrom<UserSearch>
{
    public int Id { get; set; }
    public string Parent { get; set; }
    public string ReportKey { get; set; }
    public string ReportName { get; set; }
    public string ReportId { get; set; }
    public string DefaultReport { get; set; }
    public string ShowToTenant { get; set; }
    public string Active { get; set; }
    public string UpdateBy { get; set; }
}
