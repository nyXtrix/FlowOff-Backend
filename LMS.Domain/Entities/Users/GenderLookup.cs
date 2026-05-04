using LMS.Domain.Entities.common;

namespace LMS.Domain.Entities.Users;

public class Gender : BaseEntity
{
    public string Name { get; set; } = null!;
    public int Value { get; set; }
}