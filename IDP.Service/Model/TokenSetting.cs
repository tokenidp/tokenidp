namespace IDP.Service.Model;

public class TokenSetting
{
    public string Key { get; set; }
    public string Issuer { get; set; }
    public double DurationInMinutes { get; set; }
    public double RefreshTokenExpiry { get; set; }
}
