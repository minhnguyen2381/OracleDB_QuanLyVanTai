using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Transport.Presentation.Wpf.Controls;

// Phím tắt cấp cửa sổ cho một nút (chạy Command của nút) hoặc một ô nhập (đưa con trỏ vào ô).
// Dùng: <Button c:Shortcut.Gesture="Ctrl+E" Command="{Binding ExportToExcelCommand}" />
public static class Shortcut
{
    public static readonly DependencyProperty GestureProperty = DependencyProperty.RegisterAttached(
        "Gesture", typeof(string), typeof(Shortcut), new PropertyMetadata(null, OnGestureChanged));

    // Lưu KeyBinding và cửa sổ đã gắn để gỡ đúng chỗ khi trang bị đóng
    private static readonly DependencyProperty KeyBindingProperty = DependencyProperty.RegisterAttached(
        "KeyBinding", typeof(KeyBinding), typeof(Shortcut));

    private static readonly DependencyProperty OwnerWindowProperty = DependencyProperty.RegisterAttached(
        "OwnerWindow", typeof(Window), typeof(Shortcut));

    public static string? GetGesture(DependencyObject element) => (string?)element.GetValue(GestureProperty);

    public static void SetGesture(DependencyObject element, string? value) => element.SetValue(GestureProperty, value);

    private static void OnGestureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        element.Loaded -= OnLoaded;
        element.Unloaded -= OnUnloaded;
        if (e.NewValue is string)
        {
            element.Loaded += OnLoaded;
            element.Unloaded += OnUnloaded;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        if (element.GetValue(KeyBindingProperty) is not null || Window.GetWindow(element) is not { } window
            || GetGesture(element) is not { } text)
        {
            return;
        }

        var gesture = (KeyGesture)new KeyGestureConverter().ConvertFromInvariantString(text)!;
        var binding = new KeyBinding(new RelayCommand(() => Activate(element)), gesture);
        window.InputBindings.Add(binding);
        element.SetValue(KeyBindingProperty, binding);
        element.SetValue(OwnerWindowProperty, window);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        if (element.GetValue(KeyBindingProperty) is KeyBinding binding && element.GetValue(OwnerWindowProperty) is Window window)
        {
            window.InputBindings.Remove(binding);
        }

        element.ClearValue(KeyBindingProperty);
        element.ClearValue(OwnerWindowProperty);
    }

    private static void Activate(FrameworkElement element)
    {
        if (!element.IsVisible || !element.IsEnabled)
        {
            return;
        }

        if (element is ButtonBase { Command: { } command } button)
        {
            if (command.CanExecute(button.CommandParameter))
            {
                command.Execute(button.CommandParameter);
            }
        }
        else
        {
            element.Focus();
            (element as TextBox)?.SelectAll();
        }
    }
}
