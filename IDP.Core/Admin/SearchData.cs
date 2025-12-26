using IDP.Core.Common.Model;

namespace IDP.Core.Admin;

internal class SearchData : PageInfo
{
    public IEnumerable<SearchCriteria> SearchCriterias { get; set; }
}
