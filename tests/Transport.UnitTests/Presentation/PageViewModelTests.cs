using Transport.Application.Models;
using Transport.Domain.Errors;
using Transport.Presentation.Wpf.ViewModels;
using Transport.UnitTests.Fakes;

namespace Transport.UnitTests.Presentation;

public class PageViewModelTests
{
    internal sealed class TestPage(PageServices services) : PageViewModel(services)
    {
        public Task Run(Func<Task> action, params object[] args) => RunBusyAsync(action, args);

        public Task Export(IEnumerable<int> rows) =>
            ExportAsync(rows, new[] { new ExportColumn<int>("Col_Number", x => x) }, "Title_Numbers");
    }

    private readonly PageTestContext _context = new();

    [Fact]
    public async Task RunBusyAsync_ShowsTranslatedBusinessErrorAndResetsBusy()
    {
        var page = new TestPage(_context.Services);
        var busyDuringAction = false;

        await page.Run(() =>
        {
            busyDuringAction = page.IsBusy;
            throw new BusinessRuleException(BusinessErrorCode.SeatAlreadyBooked, "Seat 5 is already booked.");
        }, 5);

        Assert.True(busyDuringAction);
        Assert.False(page.IsBusy);
        Assert.Equal(new[] { "Err_20003|5" }, _context.Notifications.Errors);
    }

    [Fact]
    public async Task ExportAsync_TranslatesTitleAndHeaders()
    {
        await new TestPage(_context.Services).Export([1, 2, 3]);

        Assert.Equal("Title_Numbers", _context.Exporter.Title);
        Assert.Equal(new[] { "Col_Number" }, _context.Exporter.Headers);
        Assert.Equal(3, _context.Exporter.RowCount);
        Assert.Equal(new[] { @"Msg_Exported|C:\temp\export.xlsx" }, _context.Notifications.Successes);
    }

    [Fact]
    public async Task ExportAsync_DoesNothingWhenSaveDialogIsCancelled()
    {
        _context.Dialogs.SaveFilePath = null;

        await new TestPage(_context.Services).Export([1]);

        Assert.Null(_context.Exporter.Title);
    }
}
