namespace ClientWebApp.Model;

public class ClaimDto
{
    public int ClaimId { get; set; }
    public int? ParentId { get; set; }
    public int UserId { get; set; }
    public int Sequence { get; set; }
    public string ClaimType { get; set; }
    public string ClaimName { get; set; }
    public string ClaimValue { get; set; }
    public string Icon { get; set; }
    public string Url { get; set; }
    public string RoleName { get; set; }
    public string ControlType { get; set; }
}
