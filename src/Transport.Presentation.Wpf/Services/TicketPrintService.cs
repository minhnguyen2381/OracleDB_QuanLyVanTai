using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Transport.Application.Models;
using Transport.Presentation.Wpf.Localization;

namespace Transport.Presentation.Wpf.Services;

// In vé khổ A6 dọc bằng FlowDocument; chọn "Microsoft Print to PDF" để ra file PDF
public sealed class TicketPrintService(ILocalizationService localization, TimeProvider time) : ITicketPrintService
{
    private const double PageWidth = 397;   // 105 mm ở 96 dpi
    private const double PageHeight = 559;  // 148 mm
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly Brush Navy = new SolidColorBrush(Color.FromRgb(0x0B, 0x3D, 0x91));

    public bool Print(TicketPrintModel ticket)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        var document = BuildDocument(ticket);
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Ticket {ticket.TicketId}");
        return true;
    }

    private FlowDocument BuildDocument(TicketPrintModel ticket)
    {
        var document = new FlowDocument
        {
            PageWidth = PageWidth,
            PageHeight = PageHeight,
            ColumnWidth = PageWidth,
            PagePadding = new Thickness(28),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
        };

        document.Blocks.Add(new Paragraph(new Run("HaUI")) { FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Navy, Margin = new Thickness(0) });
        document.Blocks.Add(new Paragraph(new Run(localization["Print_Title"]))
        {
            FontSize = 20, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Foreground = Navy, Margin = new Thickness(0, 8, 0, 0),
        });
        document.Blocks.Add(new Paragraph(new Run($"{localization["Print_TicketNo"]}: {ticket.TicketId}"))
        {
            FontSize = 16, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 4, 0, 12),
        });

        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn { Width = new GridLength(110) });
        table.Columns.Add(new TableColumn());
        var rows = new TableRowGroup();
        table.RowGroups.Add(rows);

        void AddRow(string labelKey, string value, bool emphasize = false)
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run(localization[labelKey])) { Foreground = Brushes.Gray, Margin = new Thickness(0, 3, 0, 3) }));
            row.Cells.Add(new TableCell(new Paragraph(new Run(value))
            {
                FontWeight = emphasize ? FontWeights.Bold : FontWeights.Normal,
                FontSize = emphasize ? 14 : 12,
                Margin = new Thickness(0, 3, 0, 3),
            }));
            rows.Rows.Add(row);
        }

        AddRow("Print_Route", ticket.RouteName, emphasize: true);
        AddRow("Print_Departure", ticket.DepartureTime.ToString("dd/MM/yyyy HH:mm", Vietnamese), emphasize: true);
        AddRow("Print_Arrival", ticket.ArrivalTime.ToString("dd/MM/yyyy HH:mm", Vietnamese));
        AddRow("Print_Vehicle", $"{ticket.PlateNumber} – {ticket.VehicleType}");
        AddRow("Print_Seat", ticket.SeatNo.ToString(Vietnamese), emphasize: true);
        AddRow("Print_Passenger", $"{ticket.PassengerName} – {ticket.Phone}");
        AddRow("Print_Price", string.Format(Vietnamese, "{0:N0} ₫", ticket.Price), emphasize: true);
        AddRow("Print_Status", localization[$"TicketStatus_{ticket.Status}"]);
        AddRow("Print_SoldBy", ticket.SoldBy);
        AddRow("Print_PrintedAt", time.GetLocalNow().ToString("dd/MM/yyyy HH:mm", Vietnamese));
        document.Blocks.Add(table);

        document.Blocks.Add(new Paragraph(new Run(localization["Print_Footer"]))
        {
            FontStyle = FontStyles.Italic, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 16, 0, 0),
        });
        return document;
    }
}
