using Transport.Application.Abstractions;
using Transport.Application.Models;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeExcelExporter : IExcelExporter, IExportService
{
    public string? Title { get; private set; }
    public string? FilePath { get; private set; }
    public List<string> Headers { get; } = [];
    public int RowCount { get; private set; }

    public Task ExportAsync<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string title, string filePath,
        CancellationToken ct = default)
    {
        Title = title;
        FilePath = filePath;
        Headers.AddRange(columns.Select(c => c.Header));
        RowCount = rows.Count();
        return Task.CompletedTask;
    }
}
