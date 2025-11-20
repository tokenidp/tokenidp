using Admin.Core.Application.Mappings;

namespace Identity.Application.Lookups.Queries;

public class StateLookupDto : IMapFrom<StateLookup>
{
    public string State { get; set; }
    public string Code { get; set; }
}
