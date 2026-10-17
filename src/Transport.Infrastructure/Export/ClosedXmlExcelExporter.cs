using System.Globalization;
using ClosedXML.Excel;
using Transport.Application.Abstractions;
using Transport.Application.Models;

namespace Transport.Infrastructure.Export;

// Xuất danh sách ra .xlsx: dòng 1 tiêu đề, dòng 2 thời điểm + người xuất, từ dòng 4 là bảng
internal sealed class ClosedXmlExcelExporter(IUserSession session, TimeProvider time) : IExcelExporter, IExportService
{
    private const int HeaderRow = 4;

    public Task ExportAsync<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string title, string filePath,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (columns.Count == 0)
        {
            throw new ArgumentException("At least one column must be specified.", nameof(columns));
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Data");

        var titleCell = sheet.Cell(1, 1);
        titleCell.Value = title;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 14;
        var exportedAt = time.GetLocalNow().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        sheet.Cell(2, 1).Value = $"{exportedAt} - {session.FullName} ({session.Username})";

        for (var c = 0; c < columns.Count; c++)
        {
            sheet.Cell(HeaderRow, c + 1).Value = columns[c].Header;
        }

        var row = HeaderRow + 1;
        foreach (var item in rows)
        {
            ct.ThrowIfCancellationRequested();
            for (var c = 0; c < columns.Count; c++)
            {
                var cell = sheet.Cell(row, c + 1);
                cell.Value = ToCellValue(columns[c].Value(item));
                if (columns[c].Format is { } format)
                {
                    cell.Style.NumberFormat.Format = format;
                }
            }

            row++;
        }

        if (row > HeaderRow + 1)
        {
            sheet.Range(HeaderRow, 1, row - 1, columns.Count).CreateTable();
        }
        else
        {
            sheet.Row(HeaderRow).Style.Font.Bold = true;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
        return Task.CompletedTask;
    }

    private static XLCellValue ToCellValue(object? value) => value switch
    {
        null => Blank.Value,
        string text => text,
        DateTime date => date,
        decimal number => (double)number,
        int number => (double)number,
        long number => (double)number,
        double number => number,
        bool flag => flag,
        _ => value.ToString() ?? string.Empty,
    };
}
