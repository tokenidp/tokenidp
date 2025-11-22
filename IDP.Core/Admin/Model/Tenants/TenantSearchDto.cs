using System.Linq.Expressions;

namespace IDP.Core.Admin.Model.Tenants;

internal class TenantSearchDto
{
    internal TenantSearchDto(int id,
        string tenantName,
        string tenantCode,
        string email,
        string active,
        string updateBy)
    {
        Id = id;
        TenantName = tenantName;
        TenantCode = tenantCode;
        Email = email;
        Active = active;
        UpdateBy = updateBy;
    }

    internal static Expression<Func<TenantSearch, TenantSearchDto>> Projection =>
       t => new TenantSearchDto(t.Id,
           t.TenantName,
           t.TenantCode,
           t.Email,
           t.Active,
           t.UpdatedBy);

    public int Id { get; set; }
    public string TenantName { get; set; }
    public string TenantCode { get; set; }
    public string Email { get; set; }
    public string Active { get; set; }
    public string UpdateBy { get; set; }
}
