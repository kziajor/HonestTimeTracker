namespace HonestTimeTracker.Application.Leaves;

public record LeaveDto(int Id, DateOnly StartDate, DateOnly EndDate, string? Description, int DaysCount);
