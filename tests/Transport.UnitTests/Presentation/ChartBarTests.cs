using Transport.Presentation.Wpf.ViewModels;

namespace Transport.UnitTests.Presentation;

public class ChartBarTests
{
    [Fact]
    public void Build_ScalesHeightsToTheLargestValue()
    {
        var bars = ChartBar.Build([("A", 50m), ("B", 100m), ("C", 0m)], maxHeight: 200);

        Assert.Equal(new[] { 100d, 200d, 0d }, bars.Select(b => b.Height));
        Assert.Equal("B", bars[1].Label);
    }

    [Fact]
    public void Build_AllZero_GivesZeroHeights()
    {
        Assert.All(ChartBar.Build([("A", 0m)]), b => Assert.Equal(0, b.Height));
    }
}
