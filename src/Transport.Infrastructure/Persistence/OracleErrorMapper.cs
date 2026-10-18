using System.Text.RegularExpressions;
using Transport.Domain.Errors;

namespace Transport.Infrastructure.Persistence;

// Chuyển lỗi Oracle thành BusinessRuleException theo bảng mã lỗi (spec mục 9)
internal static partial class OracleErrorMapper
{
    private static readonly Dictionary<int, BusinessErrorCode> SystemErrors = new()
    {
        [1] = BusinessErrorCode.DuplicateValue,
        [1017] = BusinessErrorCode.InvalidCredentials,
        [1031] = BusinessErrorCode.InsufficientPrivileges,
        [1920] = BusinessErrorCode.UsernameAlreadyExists,
        [2292] = BusinessErrorCode.ChildRecordExists,
        [2290] = BusinessErrorCode.CheckConstraintViolated,
        [28000] = BusinessErrorCode.AccountLocked,
        [28001] = BusinessErrorCode.PasswordExpired,
        // Verify function của PROFILE_STAFF từ chối mật khẩu (bọc ORA-20044)
        [28003] = BusinessErrorCode.InvalidPassword,
        [28007] = BusinessErrorCode.PasswordReused,
        [12541] = BusinessErrorCode.DatabaseUnavailable,
        [12514] = BusinessErrorCode.DatabaseUnavailable,
    };

    // Trả null nếu không phải lỗi đã biết (để exception gốc được ném tiếp)
    public static BusinessRuleException? Map(int number, string message)
    {
        var text = CleanMessage(message);
        if (number is >= 20000 and <= 20999 && Enum.IsDefined((BusinessErrorCode)number))
        {
            return new BusinessRuleException((BusinessErrorCode)number, text);
        }

        return SystemErrors.TryGetValue(number, out var code) ? new BusinessRuleException(code, text) : null;
    }

    // "ORA-20003: Seat 5 is already booked.\nORA-06512: ..." -> "Seat 5 is already booked."
    public static string CleanMessage(string message)
    {
        var firstLine = message.Split('\n', 2)[0].Trim();
        return OraPrefix().Replace(firstLine, string.Empty);
    }

    [GeneratedRegex(@"^ORA-\d{5}:\s*")]
    private static partial Regex OraPrefix();
}
