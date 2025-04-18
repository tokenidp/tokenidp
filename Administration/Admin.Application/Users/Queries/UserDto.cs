namespace Identity.Application.Users.Queries;

[SuppressMessage("SonarLint", "S1144", Justification = "Rich domain model and used for automapper")]
public class UserDto : IMapFrom<AppUser>
{
    public int Id { get; set; }
    public int TeneantId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string StatusId { get; set; }
    public string UserName { get; set; }
    public string Phone { get; set; }
    public bool? IsWindowsAuth { get; private set; }
    public string Address1 { get; private set; }
    public string Address2 { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string Zip { get; private set; }
    public int? ReportToId { get; private set; }
    public List<int> Roles { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<AppUser, UserDto>()
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.AppUserRoles.Select(ur => ur.RoleId).ToList()))
            .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.StatusId.ToString()));
    }
}
