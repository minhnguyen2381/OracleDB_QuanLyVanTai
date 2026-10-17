namespace Transport.Presentation.Wpf.ViewModels.Pages;

// Một hàng ghế: 2 ghế bên trái lối đi, 2 ghế bên phải
public sealed record SeatRowViewModel(IReadOnlyList<SeatViewModel> Left, IReadOnlyList<SeatViewModel> Right);
