using LinkTracker.Scrapper.Services.Updates;

namespace LinkTracker.Scrapper.Tests.Updates;

public class PreviewBuilderTests
{
    [Fact]
    public void Build_StripsHtmlDecodesEntitiesAndLimitsTo200Characters()
    {
        var text = $"<p>{new string('a', 210)}&amp;</p>";

        var preview = PreviewBuilder.Build(text);

        Assert.Equal(200, preview.Length);
        Assert.DoesNotContain("<p>", preview);
        Assert.DoesNotContain("&amp;", preview);
    }
}
