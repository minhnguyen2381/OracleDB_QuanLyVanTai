using Transport.Presentation.Wpf.ViewModels;

namespace Transport.UnitTests.Fakes;

// Gom các dịch vụ giả mà mọi ViewModel trang cần
internal sealed class PageTestContext
{
    public FakeLocalizationService Localization { get; } = new();
    public FakeNotificationService Notifications { get; } = new();
    public FakeDialogService Dialogs { get; } = new();
    public FakeUserSession Session { get; init; } = new();
    public FakeExcelExporter Exporter { get; } = new();

    public PageServices Services => new(Localization, Notifications, Dialogs, Session, Exporter);

    public static PageTestContext ForSeller() => new() { Session = FakeUserSession.Seller() };
}
