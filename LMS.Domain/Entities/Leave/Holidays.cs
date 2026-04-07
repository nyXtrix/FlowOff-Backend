using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities.Leave;

public class Holiday : BaseEntity
{
    public int TenantId {get; set;}

    public string Name {get; set;} = null!;

    public DateTime Date {get; set;}
}