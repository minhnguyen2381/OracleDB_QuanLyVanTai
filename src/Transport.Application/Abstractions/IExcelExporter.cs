using Transport.Application.Models;

namespace Transport.Application.Abstractions;

public interface IExcelExporter
{
    Task ExportAsync<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string title, string filePath,
        CancellationToken ct = default);
}
