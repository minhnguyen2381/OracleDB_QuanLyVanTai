using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;
using Transport.Domain.Errors;

namespace Transport.Application.Services;

// Bán vé: kiểm tra dữ liệu rồi gọi PKG_TICKET qua repository
public sealed class TicketService(ITicketRepository tickets)
{
    public Task<IReadOnlySet<int>> GetBookedSeatsAsync(int scheduleId, CancellationToken ct = default) =>
        tickets.GetBookedSeatsAsync(scheduleId, ct);

    public async Task<long> BookAsync(BookingRequest request, CancellationToken ct = default)
    {
        Rules.Required(request.FullName);
        Rules.Phone(request.Phone);
        if (request.SeatNo <= 0)
        {
            throw new BusinessRuleException(BusinessErrorCode.InvalidSeat, "Seat number must be positive.");
        }

        var normalized = request with
        {
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            IdCard = Rules.TrimToNull(request.IdCard),
        };
        return await tickets.BookAsync(normalized, ct);
    }

    public Task PayAsync(long ticketId, PaymentMethod method, CancellationToken ct = default) =>
        tickets.PayAsync(ticketId, method, ct);

    public async Task CancelAsync(long ticketId, string reason, CancellationToken ct = default)
    {
        Rules.Required(reason);
        await tickets.CancelAsync(ticketId, reason.Trim(), ct);
    }

    public Task<IReadOnlyList<TicketListItem>> SearchAsync(TicketSearchCriteria criteria, CancellationToken ct = default) =>
        tickets.SearchAsync(criteria with { Keyword = Rules.TrimToNull(criteria.Keyword) }, ct);

    public Task<TicketPrintModel?> GetPrintModelAsync(long ticketId, CancellationToken ct = default) =>
        tickets.GetPrintModelAsync(ticketId, ct);
}
