namespace Identity.Application.Identity.Authentication;

[SuppressMessage("SonarLint", "S1144", Justification = "Used by AutoMapper")]
public class ClaimDto : IMapFrom<UserClaim>
{
    public int ClaimId { get; private set; }
    public int? ParentId { get; private set; }
    public int UserId { get; private set; }
    public int Sequence { get; private set; }
    public string ClaimType { get; private set; }
    public string ClaimName { get; private set; }
    public string ClaimValue { get; private set; }
    public string Icon { get; private set; }
    public string Url { get; private set; }
    public string RoleName { get; private set; }
    public string ControlType { get; private set; }
    public string ReportId { get; private set; }
    public bool IsDefaultReport { get; private set; }

    public ClaimDto()
    {

    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<UserClaim, ClaimDto>()
            .ForMember(d => d.ClaimId, opt => opt.MapFrom(s => s.Id))
            .ForMember(d => d.Url, opt => opt.MapFrom(s => s.AccessUrl));
    }
}
