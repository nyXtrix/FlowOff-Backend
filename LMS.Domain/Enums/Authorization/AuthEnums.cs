namespace LMS.Domain.Enums.Authorization;

public enum ActionType
{
    VIEW,
    CREATE,
    UPDATE,
    DELETE,
    APPROVE,
    REJECT
}

public enum ScopeType
{
    SELF,
    DEPARTMENT,
    TEAM,
    ALL
}

public enum RoleType
{
    SYSTEM,
    CUSTOM
}