namespace Transport.Presentation.Wpf.Services;

// Một mục menu: khóa tiêu đề, ký tự icon (Segoe Fluent Icons), ViewModel của trang
public sealed record PageDescriptor(string TitleKey, string Glyph, Type ViewModelType, bool OperatorOnly, int Order);
