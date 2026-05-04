using LMS.Domain.Enums.Authorization;

namespace LMS.Application.Features.Organization.Roles.DTOs;

public record CreateRoleRequest(
    string Name,
    string Description,
    RoleType Type,
    Dictionary<string, PermissionModuleDto> Permissions
);

public record UpdateRoleRequest(
    string Name,
    string Description,
    bool IsActive,
    Dictionary<string, PermissionModuleDto> Permissions
);

public record PermissionModuleDto(List<string> Actions, ScopeType Scope);

public record RoleResponse(
    Guid Id,
    string Name,
    string Description,
    RoleType Type,
    bool IsActive,
    int Members,
    Dictionary<string, PermissionModuleDto> Permissions
);

public record RoleLookupResponse(string Label, Guid Value);

public record RoleStatusRequest(bool IsActive);
