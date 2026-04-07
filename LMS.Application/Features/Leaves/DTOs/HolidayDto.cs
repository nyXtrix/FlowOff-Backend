namespace LMS.Application.Features.Leaves.DTOs;

public record CreateHolidayRequest(string Name, DateTime Date);
public record HolidayResponse(int Id, string Name, DateTime Date);