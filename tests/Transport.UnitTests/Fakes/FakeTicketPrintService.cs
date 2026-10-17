using Transport.Application.Models;
using Transport.Presentation.Wpf.Services;

namespace Transport.UnitTests.Fakes;

internal sealed class FakeTicketPrintService : ITicketPrintService
{
    public List<TicketPrintModel> Printed { get; } = [];

    public bool Print(TicketPrintModel ticket)
    {
        Printed.Add(ticket);
        return true;
    }
}
