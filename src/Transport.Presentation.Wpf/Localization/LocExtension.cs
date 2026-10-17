using System.Windows.Data;
using System.Windows.Markup;

namespace Transport.Presentation.Wpf.Localization;

// {l:Loc Key}: chuỗi đã dịch, tự cập nhật khi đổi ngôn ngữ
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key) => Key = key;

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]") { Source = LocalizationService.Instance, Mode = BindingMode.OneWay }
            .ProvideValue(serviceProvider);
}
