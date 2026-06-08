using HonestTimeTracker.Application.Records;

namespace HonestTimeTracker.Application.Reports;

public record ExportRecordDto(
    int Id,
    string ProjectName,
    string TaskTitle,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MinutesSpent,
    string? Comment);

public record GetExportRecordsQuery(int Year, int? Month) : IQuery<List<ExportRecordDto>>;

public class GetExportRecordsQueryHandler(IRecordRepository recordRepository)
    : IQueryHandler<GetExportRecordsQuery, List<ExportRecordDto>>
{
    public async Task<List<ExportRecordDto>> HandleAsync(GetExportRecordsQuery query, CancellationToken ct = default)
    {
        var from = query.Month.HasValue
            ? new DateOnly(query.Year, query.Month.Value, 1)
            : new DateOnly(query.Year, 1, 1);
        var to = query.Month.HasValue
            ? new DateOnly(query.Year, query.Month.Value, DateTime.DaysInMonth(query.Year, query.Month.Value))
            : new DateOnly(query.Year, 12, 31);

        var records = await recordRepository.GetByDateRangeWithRelationsAsync(from, to, ct);

        return records
            .Where(r => r.FinishedAt.HasValue && r.MinutesSpent > 0)
            .Select(r => new ExportRecordDto(
                r.Id,
                r.Task.Project?.Name ?? "",
                r.Task.Title,
                DateOnly.FromDateTime(r.StartedAt),
                TimeOnly.FromDateTime(r.StartedAt),
                TimeOnly.FromDateTime(r.FinishedAt!.Value),
                r.MinutesSpent,
                r.Comment))
            .ToList();
    }
}
