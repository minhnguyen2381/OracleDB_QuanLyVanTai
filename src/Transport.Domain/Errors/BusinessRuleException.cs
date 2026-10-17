namespace Transport.Domain.Errors;

// Vi phạm luật nghiệp vụ; Message là thông báo tiếng Anh, giao diện dịch theo Code
public sealed class BusinessRuleException(BusinessErrorCode code, string message) : Exception(message)
{
    public BusinessErrorCode Code { get; } = code;
}
