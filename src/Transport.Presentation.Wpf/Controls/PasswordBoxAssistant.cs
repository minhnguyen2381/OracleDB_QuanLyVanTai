using System.Windows;
using System.Windows.Controls;

namespace Transport.Presentation.Wpf.Controls;

// PasswordBox.Password không bind được; thuộc tính gắn kèm này cho phép bind hai chiều với ViewModel
public static class PasswordBoxAssistant
{
    public static readonly DependencyProperty BoundPasswordProperty = DependencyProperty.RegisterAttached(
        "BoundPassword", typeof(string), typeof(PasswordBoxAssistant),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

    private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached(
        "IsUpdating", typeof(bool), typeof(PasswordBoxAssistant));

    public static string? GetBoundPassword(DependencyObject element) => (string?)element.GetValue(BoundPasswordProperty);

    public static void SetBoundPassword(DependencyObject element, string? value) => element.SetValue(BoundPasswordProperty, value);

    // Giá trị mặc định là null nên lần bind đầu (chuỗi rỗng) cũng chạy vào đây và gắn sự kiện
    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box)
        {
            return;
        }

        box.PasswordChanged -= OnPasswordChanged;
        if (!(bool)box.GetValue(IsUpdatingProperty))
        {
            box.Password = (string?)e.NewValue ?? string.Empty;
        }

        box.PasswordChanged += OnPasswordChanged;
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        var box = (PasswordBox)sender;
        box.SetValue(IsUpdatingProperty, true);
        SetBoundPassword(box, box.Password);
        box.SetValue(IsUpdatingProperty, false);
    }
}
