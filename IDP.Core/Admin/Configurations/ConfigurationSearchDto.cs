using System.Linq.Expressions;

namespace IDP.Core.Admin.Configurations;

internal class ConfigurationSearchDto
{
    internal static Expression<Func<ConfigurationSearch, ConfigurationSearchDto>> Projection =>
       t => new ConfigurationSearchDto
       {
           Id = t.Id,
           ConfigKey = t.ConfigKey,
           ConfigValue = t.ConfigValue,
           TenantName = t.TenantName,
           UserName = t.UserName,
           UpdateBy = t.UpdatedBy,
       };

    public int Id { get; set; }
    public string TenantName { get; set; }
    public string ConfigKey { get; set; }
    public string UserName { get; set; }
    public string ConfigValue { get; set; }
    public string UpdateBy { get; set; }
}
