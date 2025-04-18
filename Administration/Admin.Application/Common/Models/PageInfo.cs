namespace Identity.Application.Common.Models;

public abstract class PageInfo
{
    public int TenantId { get; set; }
    public bool IsParentTenant { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string SortOrder { get; set; }
    public string SortDirection { get; set; }
    public string SortColumn { get; set; }
    public bool SearchAll { get; set; }
}
