using ClosedXML.Excel;
using HonestTimeTracker.Application.Reports;

namespace HonestTimeTracker.Desktop.Features.Reports;

public static class ReportExcelExporter
{
    public static void Export(string filePath, List<ExportRecordDto> records, NormReportDto report)
    {
        using var workbook = new XLWorkbook();
        WriteRecordsSheet(workbook, records);
        WriteDaysSheet(workbook, report);
        workbook.SaveAs(filePath);
    }

    private static void WriteRecordsSheet(XLWorkbook workbook, List<ExportRecordDto> records)
    {
        var sheet = workbook.Worksheets.Add("Records");

        sheet.Cell(1, 1).Value = "Id";
        sheet.Cell(1, 2).Value = "Project";
        sheet.Cell(1, 3).Value = "Task";
        sheet.Cell(1, 4).Value = "Date";
        sheet.Cell(1, 5).Value = "Start";
        sheet.Cell(1, 6).Value = "End";
        sheet.Cell(1, 7).Value = "Minutes";
        sheet.Cell(1, 8).Value = "Comment";
        StyleHeader(sheet.Range(1, 1, 1, 8));

        for (var i = 0; i < records.Count; i++)
        {
            var row = i + 2;
            var r = records[i];
            sheet.Cell(row, 1).Value = r.Id;
            sheet.Cell(row, 2).Value = r.ProjectName;
            sheet.Cell(row, 3).Value = r.TaskTitle;
            sheet.Cell(row, 4).Value = r.Date.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 4).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(row, 5).Value = r.StartTime.ToTimeSpan();
            sheet.Cell(row, 5).Style.DateFormat.Format = "hh:mm";
            sheet.Cell(row, 6).Value = r.EndTime.ToTimeSpan();
            sheet.Cell(row, 6).Style.DateFormat.Format = "hh:mm";
            sheet.Cell(row, 7).Value = r.MinutesSpent;
            sheet.Cell(row, 8).Value = r.Comment ?? "";
        }

        sheet.Columns().AdjustToContents();
    }

    private static void WriteDaysSheet(XLWorkbook workbook, NormReportDto report)
    {
        var sheet = workbook.Worksheets.Add("Days");

        sheet.Cell(1, 1).Value = "Date";
        sheet.Cell(1, 2).Value = "Day";
        sheet.Cell(1, 3).Value = "Type";
        sheet.Cell(1, 4).Value = "Required (h)";
        sheet.Cell(1, 5).Value = "Actual (h)";
        sheet.Cell(1, 6).Value = "Delta (h)";
        StyleHeader(sheet.Range(1, 1, 1, 6));

        for (var i = 0; i < report.Days.Count; i++)
        {
            var row = i + 2;
            var d = report.Days[i];
            var actual = Math.Round(d.ActualHours, 2);
            var delta = Math.Round(actual - d.RequiredHours, 2);

            sheet.Cell(row, 1).Value = d.Date.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 1).Style.DateFormat.Format = "yyyy-mm-dd";
            sheet.Cell(row, 2).Value = d.Date.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture);
            sheet.Cell(row, 3).Value = d.IsWeekend ? "Weekend" : d.IsLeave ? "Leave" : "Work";
            sheet.Cell(row, 4).Value = d.RequiredHours;
            sheet.Cell(row, 4).Style.NumberFormat.Format = "0.00";
            sheet.Cell(row, 5).Value = actual;
            sheet.Cell(row, 5).Style.NumberFormat.Format = "0.00";
            sheet.Cell(row, 6).Value = delta;
            sheet.Cell(row, 6).Style.NumberFormat.Format = "0.00";
        }

        sheet.Columns().AdjustToContents();
    }

    private static void StyleHeader(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
    }
}
