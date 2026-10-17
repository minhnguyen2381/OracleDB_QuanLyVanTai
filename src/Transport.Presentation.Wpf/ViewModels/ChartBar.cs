namespace Transport.Presentation.Wpf.ViewModels;

// Một cột của biểu đồ cột vẽ bằng ItemsControl (BarChartStyle)
public sealed record ChartBar(string Label, decimal Value, double Height)
{
    public static IReadOnlyList<ChartBar> Build(IEnumerable<(string Label, decimal Value)> points, double maxHeight = 150)
    {
        var list = points.ToList();
        var max = list.Count == 0 ? 0 : list.Max(p => p.Value);
        return [.. list.Select(p => new ChartBar(p.Label, p.Value, max <= 0 ? 0 : (double)(p.Value / max) * maxHeight))];
    }
}
