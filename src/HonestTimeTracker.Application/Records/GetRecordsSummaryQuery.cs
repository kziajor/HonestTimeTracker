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

public record GetRecordsSummaryQuery(DateOnly? From, DateOnly? To) : IQuery<RecordsSummaryDto?>;

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
        var today = DateOnly.FromDateTime(DateTime.Today);

        DateOnly from;
        DateOnly to;

        if (query.From.HasValue && query.To.HasValue)
        {
            from = query.From.Value;
            to = query.To.Value > today ? today : query.To.Value;
        }
        else
        {
            var earliest = await recordRepository.GetEarliestDateAsync(ct);
            if (!earliest.HasValue) return null;
            from = earliest.Value;
            to = today;
        }

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

        var records = await recordRepository.GetByDateRangeAsync(from, to, ct);
        var actualByDay = records
            .Where(r => r.MinutesSpent > 0)
            .GroupBy(r => DateOnly.FromDateTime(r.StartedAt))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.MinutesSpent) / 60.0);

        double actualHours = 0;
        double requiredHours = 0;
        int workingDays = 0;
        int leaveDayCount = 0;
        int weekendDays = 0;

        var current = from;
        while (current <= to)
        {
            var isWeekend = current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isLeave = leaveDays.Contains(current);
            var required = (isWeekend || isLeave) ? 0.0 : settings.DailyWorkHours;
            var actual = actualByDay.GetValueOrDefault(current, 0.0);

            if (current == today && actual < required)
                actual = required;

            actualHours += actual;
            requiredHours += required;

            if (isWeekend) weekendDays++;
            else if (isLeave) leaveDayCount++;
            else workingDays++;

            current = current.AddDays(1);
        }

        return new RecordsSummaryDto(
            actualHours,
            requiredHours,
            actualHours - requiredHours,
            workingDays,
            leaveDayCount,
            weekendDays);
    }
}
