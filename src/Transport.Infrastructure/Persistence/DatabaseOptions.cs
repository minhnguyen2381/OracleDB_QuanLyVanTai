namespace Transport.Infrastructure.Persistence;

// Mục "Database" trong appsettings.json
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string DataSource { get; set; } = "localhost:1521/XEPDB1";

    // Schema chứa bảng/package; mọi phiên được ALTER SESSION SET CURRENT_SCHEMA về đây
    public string Schema { get; set; } = "TRANSPORT";
}
