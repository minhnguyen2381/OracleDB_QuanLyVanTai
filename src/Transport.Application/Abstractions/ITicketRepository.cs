using Transport.Application.Models;
using Transport.Domain.Enums;

namespace Transport.Application.Abstractions;

public interface ITicketRepository
{
    Task<IReadOnlySet<int>> GetBookedSeatsAsync(int scheduleId, CancellationToken ct = default);
    // PKG_TICKET.BOOK_TICKET, trả về mã vé
    Task<long> BookAsync(BookingRequest request, CancellationToken ct = default);
    Task PayAsync(long ticketId, PaymentMethod method, CancellationToken ct = default);
    Task CancelAsync(long ticketId, string reason, CancellationToken ct = default);
    Task<IReadOnlyList<TicketListItem>> SearchAsync(TicketSearchCriteria criteria, CancellationToken ct = default);
    Task<TicketPrintModel?> GetPrintModelAsync(long ticketId, CancellationToken ct = default);
}
