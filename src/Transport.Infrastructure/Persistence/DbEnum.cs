using System.Text.RegularExpressions;
using Transport.Domain.Enums;

namespace Transport.Infrastructure.Persistence;

// Chuyển enum C# sang giá trị trạng thái trong Oracle (VD TicketSeller -> TICKET_SELLER).
// Chiều đọc do Dapper tự Enum.Parse (không phân biệt hoa thường).
internal static partial class DbEnum
{
    public static string ToDb<TEnum>(TEnum value) where TEnum : struct, Enum =>
        WordBoundary().Replace(value.ToString(), "$1_$2").ToUpperInvariant();

    public static string RoleToDb(StaffRole role) => "ROLE_" + ToDb(role);

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex WordBoundary();
}
