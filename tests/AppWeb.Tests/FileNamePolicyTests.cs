using AppWeb.Core.Validation;

namespace AppWeb.Tests;

public sealed class FileNamePolicyTests
{
    [Theory]
    [InlineData("Notion", "Notion")]
    [InlineData("  Meu App  ", "Meu App")]
    [InlineData("Bad:Name?", "Bad Name")]
    [InlineData("CON", "CON App")]
    [InlineData("", "Web App")]
    public void Sanitize_ProducesSafeNames(string input, string expected)
    {
        Assert.Equal(expected, FileNamePolicy.Sanitize(input));
    }

    [Fact]
    public void Sanitize_TrimsTrailingDotsAndSpaces()
    {
        Assert.Equal("App", FileNamePolicy.Sanitize("App...  "));
    }

    [Fact]
    public void Sanitize_RespectsMaximumLength()
    {
        var result = FileNamePolicy.Sanitize(new string('a', 100), maxLength: 20);

        Assert.Equal(20, result.Length);
    }
}
