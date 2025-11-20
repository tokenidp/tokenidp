using Admin.Core.Application.Mappings;

namespace Identity.Application.Reports.Queries;

public class ReportDto : IMapFrom<AppClaim>
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public string ReportKey { get; set; }
    public string ReportName { get; set; }
    public string AccessUrl { get; set; }
    public string ControlType { get; set; }
    public string ReportId { get; set; }
    public bool IsDefaultReport { get; set; }
    public bool ShowToTenant { get; set; }
    public bool IsActive { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<AppClaim, ReportDto>()
            .ForMember(d => d.ReportKey, opt => opt.MapFrom(s => s.ClaimType))
            .ForMember(d => d.ReportName, opt => opt.MapFrom(s => s.ClaimName));
    }
}
