
namespace TokenIDP.Core.Admin.Users;

public class UserLookups
{
    public IEnumerable<LookupItem> Roles { get; set; }

    public IEnumerable<LookupItem> UserStatuses { get; set; }

    public IEnumerable<LookupItem> AddressTypes { get; set; }
}
