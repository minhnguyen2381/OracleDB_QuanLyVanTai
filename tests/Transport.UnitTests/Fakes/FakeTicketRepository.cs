using Transport.Application.Abstractions;
using Transport.Application.Models;
using Transport.Domain.Enums;
using Transport.Domain.Errors;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeTicketRepository : ITicketRepository
{
    public HashSet<int> BookedSeats { get; } = [];
    public long NextTicketId { get; set; } = 1;
    public BookingRequest? LastBooking { get; private set; }
    public BusinessRuleException? BookError { get; set; }
    public List<(long TicketId, PaymentMethod Method)> Payments { get; } = [];
    public List<(long TicketId, string Reason)> Cancellations { get; } = [];
    public TicketSearchCriteria? LastSearch { get; private set; }
    public List<TicketListItem> SearchResult { get; } = [];
    public TicketPrintModel? PrintModel { get; set; }

    public Task<IReadOnlySet<int>> GetBookedSeatsAsync(int scheduleId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlySet<int>>(BookedSeats.ToHashSet());

    public Task<long> BookAsync(BookingRequest request, CancellationToken ct = default)
    {
        if (BookError is not null)
        {
            throw BookError;
        }

        LastBooking = request;
        BookedSeats.Add(request.SeatNo);
        return Task.FromResult(NextTicketId++);
    }

    public Task PayAsync(long ticketId, PaymentMethod method, CancellationToken ct = default)
    {
        Payments.Add((ticketId, method));
        return Task.CompletedTask;
    }

    public Task CancelAsync(long ticketId, string reason, CancellationToken ct = default)
    {
        Cancellations.Add((ticketId, reason));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TicketListItem>> SearchAsync(TicketSearchCriteria criteria, CancellationToken ct = default)
    {
        LastSearch = criteria;
        return Task.FromResult<IReadOnlyList<TicketListItem>>(SearchResult.ToList());
    }

    public Task<TicketPrintModel?> GetPrintModelAsync(long ticketId, CancellationToken ct = default) =>
        Task.FromResult(PrintModel);
}
