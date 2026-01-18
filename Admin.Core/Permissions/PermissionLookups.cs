using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Admin.Core.Permissions;

internal sealed class PermissionLookups
{
    public List<LookupItem> ParentMenus { get; init; } = new();
    public List<LookupItem> ControlTypes { get; init; } = new();
}


