namespace TokenIDP.Core.Foundation.Primitives;

public class SearchData : PageInfo
{
    public List<SearchCriteria> SearchCriterias { get; set; } = new List<SearchCriteria>();
}

