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
    TEAM,
    ALL
}