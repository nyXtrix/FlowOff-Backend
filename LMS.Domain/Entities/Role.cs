using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities;

public class Role : BaseEntity
{
    public int TenantId {get; set;}
    
    public string Name {get; set;} = null!;

    public ICollection<UserRole> UserRoles {get; set;} = null!;
    public ICollection<Position> Positions {get; set;} = null!;
}