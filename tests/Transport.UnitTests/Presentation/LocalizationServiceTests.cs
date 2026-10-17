using Transport.Domain.Errors;
using Transport.Presentation.Wpf.Localization;

namespace Transport.UnitTests.Presentation;

public class LocalizationServiceTests
{
    [Fact]
    public void Indexer_ReturnsTextForCurrentLanguage()
    {
        var localization = new LocalizationService();

        localization.SetLanguage("vi");
        Assert.Equal("Đăng nhập", localization["Login_Title"]);

        localization.SetLanguage("en");
        Assert.Equal("Sign in", localization["Login_Title"]);
        Assert.Equal("en", localization.CurrentLanguage);
    }

    [Fact]
    public void Indexer_ReturnsKeyWhenMissing()
    {
        Assert.Equal("No_Such_Key", new LocalizationService()["No_Such_Key"]);
    }

    [Fact]
    public void SetLanguage_RaisesIndexerChange()
    {
        var localization = new LocalizationService();
        var changed = new List<string?>();
        localization.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        localization.SetLanguage("en");

        Assert.Contains("Item[]", changed);
    }

    [Fact]
    public void Translate_FormatsKnownErrorAndFallsBackToEnglishMessage()
    {
        var localization = new LocalizationService();
        localization.SetLanguage("vi");

        Assert.Equal("Ghế 5 đã có người đặt.",
            localization.Translate(new BusinessRuleException(BusinessErrorCode.SeatAlreadyBooked, "Seat 5 is already booked."), 5));
        Assert.Equal("Raw message",
            localization.Translate(new BusinessRuleException((BusinessErrorCode)12345, "Raw message")));
    }

    [Fact]
    public void Format_ReturnsTemplateWhenArgumentsAreMissing()
    {
        var localization = new LocalizationService();
        localization.SetLanguage("en");

        Assert.Equal("Exported to {0}", localization.Format("Msg_Exported"));
        Assert.Equal("Exported to C:\\a.xlsx", localization.Format("Msg_Exported", "C:\\a.xlsx"));
    }
}
