namespace Identity.Application.Configurations.Queries;

public class ConfigurationSearchDto : IMapFrom<UserSearch>
{
    public int Id { get; set; }
    public string TenantName { get; set; }
    public string ConfigKey { get; set; }
    public string UserName { get; set; }
    public string ConfigValue { get; set; }
    public string UpdateBy { get; set; }
}
