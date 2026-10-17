using System.IO;
using System.Xml.Linq;
using Transport.Domain.Errors;

namespace Transport.UnitTests.Presentation;

public class ResourceParityTests
{
    private static HashSet<string> Keys(string fileName) =>
        XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Resources", fileName))
            .Root!.Elements("data")
            .Select(e => (string)e.Attribute("name")!)
            .ToHashSet();

    [Fact]
    public void EnglishAndVietnameseHaveTheSameKeys()
    {
        var en = Keys("Strings.resx");
        var vi = Keys("Strings.vi.resx");

        Assert.Empty(en.Except(vi));
        Assert.Empty(vi.Except(en));
    }

    [Fact]
    public void EveryBusinessErrorCodeHasATranslation()
    {
        var en = Keys("Strings.resx");

        var missing = Enum.GetValues<BusinessErrorCode>().Select(c => $"Err_{(int)c}").Where(k => !en.Contains(k));

        Assert.Empty(missing);
    }
}
