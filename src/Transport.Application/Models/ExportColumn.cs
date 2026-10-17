namespace Transport.Application.Models;

// Một cột khi xuất Excel; Format là định dạng số/ngày của Excel (VD "#,##0")
public sealed record ExportColumn<T>(string Header, Func<T, object?> Value, string? Format = null);
