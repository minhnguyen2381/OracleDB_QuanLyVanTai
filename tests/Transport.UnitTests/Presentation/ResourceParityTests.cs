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

    [Fact]
    public void AllReferencedResourceKeysInXamlExistInResx()
    {
        var keys = Keys("Strings.resx");
        var presentationDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Transport.Presentation.Wpf"));
        Assert.True(Directory.Exists(presentationDir), $"Presentation dir not found at {presentationDir}");

        var missing = new List<string>();

        foreach (var file in Directory.EnumerateFiles(presentationDir, "*.xaml", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"\{l:Loc\s+([A-Za-z0-9_]+)\}");
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                var k = m.Groups[1].Value;
                if (!keys.Contains(k))
                {
                    missing.Add($"XAML ({Path.GetFileName(file)}): {k}");
                }
            }
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void AllReferencedResourceKeysInCSharpExistInResx()
    {
        var keys = Keys("Strings.resx");
        var presentationDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Transport.Presentation.Wpf"));
        Assert.True(Directory.Exists(presentationDir), $"Presentation dir not found at {presentationDir}");

        var missing = new List<string>();

        foreach (var file in Directory.EnumerateFiles(presentationDir, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            // Loc["..."] or Loc.Format("...")
            var locMatches = System.Text.RegularExpressions.Regex.Matches(content, @"Loc(?:\[|\.Format\()\s*""([A-Za-z0-9_]+)""");
            foreach (System.Text.RegularExpressions.Match m in locMatches)
            {
                var k = m.Groups[1].Value;
                if (!keys.Contains(k))
                {
                    missing.Add($"CS Loc ({Path.GetFileName(file)}): {k}");
                }
            }

            // ErrorMessage = "..."
            var errMatches = System.Text.RegularExpressions.Regex.Matches(content, @"ErrorMessage\s*=\s*""([A-Za-z0-9_]+)""");
            foreach (System.Text.RegularExpressions.Match m in errMatches)
            {
                var k = m.Groups[1].Value;
                if (!keys.Contains(k))
                {
                    missing.Add($"CS ErrorMessage ({Path.GetFileName(file)}): {k}");
                }
            }

            // ExportColumn keys: new("Key", ...)
            var expMatches = System.Text.RegularExpressions.Regex.Matches(content, @"new\s*\(\s*""([A-Za-z0-9_]+)""\s*,\s*[^)]+\)");
            foreach (System.Text.RegularExpressions.Match m in expMatches)
            {
                var k = m.Groups[1].Value;
                if (k.Contains('_') && !keys.Contains(k))
                {
                    missing.Add($"CS ExportColumn ({Path.GetFileName(file)}): {k}");
                }
            }
        }

        Assert.Empty(missing);
    }
}

