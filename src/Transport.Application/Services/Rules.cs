using System.Text.RegularExpressions;
using Transport.Application.Abstractions;
using Transport.Domain.Errors;

namespace Transport.Application.Services;

// Luật kiểm tra dữ liệu phía client, chạy trước khi gọi Oracle
internal static partial class Rules
{
    public const int MinPasswordLength = 8;

    public static void Required(params string?[] values)
    {
        if (values.Any(string.IsNullOrWhiteSpace))
        {
            throw new BusinessRuleException(BusinessErrorCode.RequiredField, "A required field is empty.");
        }
    }

    public static void Phone(string? phone)
    {
        if (phone is null || !PhonePattern().IsMatch(phone.Trim()))
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidPhone, "Phone must be 10 digits starting with 0.");
        }
    }

    public static void OptionalPhone(string? phone)
    {
        if (!string.IsNullOrWhiteSpace(phone))
        {
            Phone(phone);
        }
    }

    public static void Operator(IUserSession session)
    {
        if (!session.IsOperator)
        {
            throw new BusinessRuleException(BusinessErrorCode.OperatorOnly, "Only operators can perform this action.");
        }
    }

    // Cùng luật với PKG_STAFF.valid_username
    public static void Username(string? username)
    {
        if (username is null || !UsernamePattern().IsMatch(username))
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidUsername, "Invalid username.");
        }
    }

    // Cùng luật với SYS.STAFF_PASSWORD_VERIFY (PROFILE_STAFF) và dấu nháy kép bị PKG_STAFF chặn
    public static void Password(string? password, string? username)
    {
        if (password is null || password.Length < MinPasswordLength || password.Contains('"')
            || !password.Any(char.IsAsciiLetterUpper) || !password.Any(char.IsAsciiLetterLower)
            || !password.Any(char.IsAsciiDigit) || password.All(char.IsAsciiLetterOrDigit)
            || (!string.IsNullOrWhiteSpace(username)
                && password.Contains(username.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidPassword,
                "Password does not meet the complexity rules.");
        }
    }

    public static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex(@"^0\d{9}$")]
    private static partial Regex PhonePattern();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]{2,29}$")]
    private static partial Regex UsernamePattern();
}
