namespace Identity.Application.Tenants.Queries;

public class TenantSearchDto : IMapFrom<TenantSearch>
{
    public int Id { get; set; }
    public string TenantName { get; set; }
    public string TenantCode { get; set; }
    public string Email { get; set; }
    public string Active { get; set; }
    public string UpdateBy { get; set; }
}
