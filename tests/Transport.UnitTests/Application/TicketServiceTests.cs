using Transport.Application.Models;
using Transport.Application.Services;
using Transport.Domain.Errors;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Application;

public class TicketServiceTests
{
    private readonly FakeTicketRepository _repository = new() { NextTicketId = 42 };
    private readonly TicketService _service;

    public TicketServiceTests() => _service = new TicketService(_repository);

    [Fact]
    public async Task BookAsync_TrimsInputAndReturnsTicketId()
    {
        var id = await _service.BookAsync(new BookingRequest(7, "  Nguyễn Văn A ", " 0912345678 ", "   ", 5));

        Assert.Equal(42, id);
        Assert.Equal(new BookingRequest(7, "Nguyễn Văn A", "0912345678", null, 5), _repository.LastBooking);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1912345678")]
    [InlineData("09123456789")]
    [InlineData("09123abc78")]
    public async Task BookAsync_RejectsInvalidPhone(string phone)
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.BookAsync(new BookingRequest(7, "Nguyễn Văn A", phone, null, 5)));

        Assert.Equal(BusinessErrorCode.InvalidPhone, ex.Code);
        Assert.Null(_repository.LastBooking);
    }

    [Fact]
    public async Task BookAsync_RejectsEmptyName()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.BookAsync(new BookingRequest(7, " ", "0912345678", null, 5)));

        Assert.Equal(BusinessErrorCode.RequiredField, ex.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task BookAsync_RejectsNonPositiveSeat(int seat)
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.BookAsync(new BookingRequest(7, "Nguyễn Văn A", "0912345678", null, seat)));

        Assert.Equal(BusinessErrorCode.InvalidSeat, ex.Code);
    }

    [Fact]
    public async Task CancelAsync_RequiresReasonAndTrimsIt()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CancelAsync(5, "  "));
        Assert.Equal(BusinessErrorCode.RequiredField, ex.Code);

        await _service.CancelAsync(5, " Khách đổi ý ");
        Assert.Equal((5L, "Khách đổi ý"), Assert.Single(_repository.Cancellations));
    }

    [Fact]
    public async Task SearchAsync_TurnsBlankKeywordIntoNull()
    {
        await _service.SearchAsync(new TicketSearchCriteria { Keyword = "   " });

        Assert.Null(_repository.LastSearch!.Keyword);
    }
}
