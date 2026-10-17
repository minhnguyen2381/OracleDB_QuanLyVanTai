using System.IO;
using ClosedXML.Excel;
using Transport.Application.Models;
using Transport.Infrastructure.Export;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Infrastructure;

public class ClosedXmlExcelExporterTests
{
    private sealed record Row(string Name, decimal Price, DateTime Departure);

    [Fact]
    public async Task ExportAsync_WritesTitleHeadersAndFormattedRows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"export-test-{Guid.NewGuid():N}.xlsx");
        var exporter = new ClosedXmlExcelExporter(new FakeUserSession(),
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 17, 9, 30, 0, TimeSpan.Zero)));
        var columns = new[]
        {
            new ExportColumn<Row>("Tuyến", r => r.Name),
            new ExportColumn<Row>("Giá", r => r.Price, "#,##0"),
            new ExportColumn<Row>("Khởi hành", r => r.Departure, "dd/MM/yyyy HH:mm"),
        };
        var rows = new[] { new Row("Hà Nội - Lào Cai", 280000m, new DateTime(2026, 10, 20, 7, 0, 0)) };

        try
        {
            await exporter.ExportAsync(rows, columns, "Danh sách chuyến", path);

            using var workbook = new XLWorkbook(path);
            var sheet = workbook.Worksheet(1);
            Assert.Equal("Danh sách chuyến", sheet.Cell(1, 1).GetString());
            Assert.Equal("17/10/2026 09:30 - Nguyễn Văn Điều (OPERATOR01)", sheet.Cell(2, 1).GetString());
            Assert.Equal("Giá", sheet.Cell(4, 2).GetString());
            Assert.Equal("Hà Nội - Lào Cai", sheet.Cell(5, 1).GetString());
            Assert.Equal(280000d, sheet.Cell(5, 2).GetDouble());
            Assert.Equal("#,##0", sheet.Cell(5, 2).Style.NumberFormat.Format);
            Assert.Equal(new DateTime(2026, 10, 20, 7, 0, 0), sheet.Cell(5, 3).GetDateTime());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
