using Transport.Application.Models;

namespace Transport.Presentation.Wpf.Services;

public interface ITicketPrintService
{
    // Mở hộp thoại in; false nếu người dùng hủy
    bool Print(TicketPrintModel ticket);
}
