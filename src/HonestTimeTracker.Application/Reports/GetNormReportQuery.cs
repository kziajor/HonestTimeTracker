using HonestTimeTracker.Application.Leaves;
using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Application.Settings;

namespace HonestTimeTracker.Application.Reports;

public record NormReportDayEntry(
    DateOnly Date,
    bool IsWeekend,
    bool IsLeave,
    double RequiredHours,
    double ActualHours);

public record NormReportDto(
    int Year,
    int? Month,
    double RequiredHours,
    double ActualHours,
    double OvertimeHours,
    int WorkingDays,
    int LeaveDays,
    int WeekendDays,
    List<NormReportDayEntry> Days);

public record GetNormReportQuery(int Year, int? Month) : IQuery<NormReportDto>;

public class GetNormReportQueryHandler(
    IRecordRepository recordRepository,
    ILeaveRepository leaveRepository,
    ISettingsRepository settingsRepository)
    : IQueryHandler<GetNormReportQuery, NormReportDto>
{
    public async Task<NormReportDto> HandleAsync(GetNormReportQuery query, CancellationToken ct = default)
    {
        var settings = await settingsRepository.GetAsync(ct);
        var leaves = await leaveRepository.GetAllAsync(ct);

        var from = query.Month.HasValue
            ? new DateOnly(query.Year, query.Month.Value, 1)
            : new DateOnly(query.Year, 1, 1);
        var to = query.Month.HasValue
            ? new DateOnly(query.Year, query.Month.Value, DateTime.DaysInMonth(query.Year, query.Month.Value))
            : new DateOnly(query.Year, 12, 31);

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (to > today) to = today;

        var earliest = await recordRepository.GetEarliestDateAsync(ct);
        if (earliest.HasValue && earliest.Value > from)
            from = earliest.Value;

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

        var days = new List<NormReportDayEntry>();
        var current = from;
        while (current <= to)
        {
            var isWeekend = current.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isLeave = leaveDays.Contains(current);
            var required = (isWeekend || isLeave) ? 0.0 : settings.DailyWorkHours;
            var actual = actualByDay.GetValueOrDefault(current, 0.0);
            if (current == today && actual < required)
                actual = required;
            days.Add(new NormReportDayEntry(current, isWeekend, isLeave, required, actual));
            current = current.AddDays(1);
        }

        return new NormReportDto(
            query.Year,
            query.Month,
            days.Sum(d => d.RequiredHours),
            days.Sum(d => d.ActualHours),
            days.Sum(d => d.ActualHours) - days.Sum(d => d.RequiredHours),
            days.Count(d => !d.IsWeekend && !d.IsLeave),
            days.Count(d => d.IsLeave),
            days.Count(d => d.IsWeekend),
            days);
    }
}
