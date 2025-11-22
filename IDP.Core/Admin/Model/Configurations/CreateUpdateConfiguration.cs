namespace IDP.Core.Admin.Model.Configurations;

internal class CreateUpdateConfiguration
{
    public int Id { get; set; }
    public string ConfigKey { get; set; }
    public string ConfigValue { get; set; }
    public bool? IsDisplay { get; set; }
    public bool IsEditable { get; set; }
}