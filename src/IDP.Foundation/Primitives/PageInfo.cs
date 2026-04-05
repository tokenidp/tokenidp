namespace IDP.Foundation.Primitives;

public abstract class PageInfo
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string SortOrder { get; set; }
    public string SortDirection { get; set; }
    public string SortColumn { get; set; }
    public bool SearchAll { get; set; }
}
