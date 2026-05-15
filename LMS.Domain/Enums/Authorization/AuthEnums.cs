using System.Text.Json.Serialization;

namespace LMS.Domain.Enums.Authorization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActionType
{
    VIEW,
    CREATE,
    UPDATE,
    DELETE,
    APPROVE,
    REJECT
}

[JsonConverter(typeof(JsonStringEnumConverter))]
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