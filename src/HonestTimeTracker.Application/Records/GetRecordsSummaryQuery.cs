using HonestTimeTracker.Application.Leaves;
using HonestTimeTracker.Application.Settings;

namespace HonestTimeTracker.Application.Records;

public record RecordsSummaryDto(
    double ActualHours,
    double RequiredHours,
    double OvertimeHours,
    int WorkingDays,
    int LeaveDays,
    int WeekendDays);

public record GetRecordsSummaryQuery(DateOnly Date) : IQuery<RecordsSummaryDto?>;

public class GetRecordsSummaryQueryHandler(
    IRecordRepository recordRepository,
    ILeaveRepository leaveRepository,
    ISettingsRepository settingsRepository)
    : IQueryHandler<GetRecordsSummaryQuery, RecordsSummaryDto?>
{
    public async Task<RecordsSummaryDto?> HandleAsync(GetRecordsSummaryQuery query, CancellationToken ct = default)
    {
        var settings = await settingsRepository.GetAsync(ct);
        var leaves = await leaveRepository.GetAllAsync(ct);

        var leaveDays = new HashSet<DateOnly>();
        foreach (var leave in leaves)
        {
            var d = leave.StartDate;
            while (d <= leave.EndDate)
            {
                leaveDays.Add(d);
                d = d.AddDays(1);
            }
        }

        var records = await recordRepository.GetByDateRangeAsync(query.Date, query.Date, ct);
        var actualHours = records
            .Where(r => r.MinutesSpent > 0)
            .Sum(r => r.MinutesSpent) / 60.0;

        var isWeekend = query.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var isLeave = leaveDays.Contains(query.Date);
        var requiredHours = (isWeekend || isLeave) ? 0.0 : settings.DailyWorkHours;

        return new RecordsSummaryDto(
            actualHours,
            requiredHours,
            actualHours - requiredHours,
            isWeekend || isLeave ? 0 : 1,
            isLeave ? 1 : 0,
            isWeekend ? 1 : 0);
    }
}
