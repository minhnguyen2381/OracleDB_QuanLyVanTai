namespace Transport.Presentation.Wpf.ViewModels;

// Một lựa chọn trong combobox lọc; Value = null nghĩa là "Tất cả"
public sealed record FilterOption<T>(T Value, string Label);
