using System.Text.Json.Serialization;

namespace LMS.Domain.Enums.Policy;

public enum PolicyScope
{
    Org = 1,
    Location,
    Department,
    Role,
    Employee
}

public enum WeekOfMonth
{
    All = 0,
    First,
    Second,
    Third,
    Fourth,
    Last
}

public enum AccuralFrequency
{
    Monthly,
    Quarterly,
    Yearly
}

public enum RoundingRule
{
    None,
    HalfDay,
    Hour
}

public enum LeavePolicyType
{
    Allocation,
    Usage,
    Balance,
    WeekOff
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkflowApprovalMode
{
    Sequential,
    AnyOne,
    Parallel
}
