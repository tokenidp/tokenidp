using System.Linq.Expressions;

namespace IDP.Core.Admin.Model.Configurations;

internal class ConfigurationSearchDto
{
    public static Expression<Func<ConfigurationSearch, ConfigurationSearchDto>> Projection =>
       t => new ConfigurationSearchDto
       {
           Id = t.Id,
           ConfigKey = t.ConfigKey,
           ConfigValue = t.ConfigValue,
           TenantName = t.TenantName,
           UserName = t.UserName,
           UpdateBy = t.UpdateBy,
       };

    public int Id { get; set; }
    public string TenantName { get; set; }
    public string ConfigKey { get; set; }
    public string UserName { get; set; }
    public string ConfigValue { get; set; }
    public string UpdateBy { get; set; }
}
