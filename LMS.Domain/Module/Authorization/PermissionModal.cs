

using LMS.Domain.Enums.Authorization;

namespace LMS.Domain.Module.Authorization;

public class ModulePermission
{
    public List<ActionType> Actions { get; set; } = new();
    public ScopeType Scope { get; set; }
}

public class AppPermissions : Dictionary<string, ModulePermission> { }

public class ResourceContext
{
    public Guid UserId { get; set; }
    public Guid? ManagerId { get; set; }
}