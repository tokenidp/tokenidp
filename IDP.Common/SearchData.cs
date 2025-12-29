using IDP.Common.Model;

namespace Admin.Core;

public class SearchData : PageInfo
{
    public IEnumerable<SearchCriteria> SearchCriterias { get; set; }
}
