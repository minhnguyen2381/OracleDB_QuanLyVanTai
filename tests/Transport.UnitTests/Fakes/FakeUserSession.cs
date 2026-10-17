using Transport.Application.Abstractions;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeUserSession : IUserSession
{
    public string Username { get; set; } = "OPERATOR01";
    public string FullName { get; set; } = "Nguyễn Văn Điều";
    public bool IsOperator { get; set; } = true;
    public bool IsAuthenticated { get; set; } = true;

    public static FakeUserSession Seller() => new() { Username = "SELLER01", FullName = "Trần Thị Bán", IsOperator = false };
}
