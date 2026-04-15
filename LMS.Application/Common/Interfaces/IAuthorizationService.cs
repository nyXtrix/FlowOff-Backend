using LMS.Domain.Module.Authorization;
using LMS.Domain.Enums.Authorization;

namespace LMS.Application.Common.Interfaces;

public interface ILmsAuthorizationService
{
    bool CanPerform(AppPermissions permissions, string module, ActionType action, ResourceContext resource = null);
}
