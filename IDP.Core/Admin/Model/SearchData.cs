namespace IDP.Core.Admin.Model;

internal class SearchData : PageInfo
{
    public IEnumerable<SearchCriteria> SearchCriterias { get; set; }
}
