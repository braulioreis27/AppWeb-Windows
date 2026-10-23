using AppWeb.Core.Validation;

namespace AppWeb.Tests;

public sealed class UrlPolicyTests
{
    [Theory]
    [InlineData("example.com", "https://example.com/")]
    [InlineData("https://example.com/path?q=1", "https://example.com/path?q=1")]
    [InlineData("http://localhost:8080", "http://localhost:8080/")]
    public void TryNormalize_AcceptsHttpAndHttps(string input, string expected)
    {
        var success = UrlPolicy.TryNormalize(input, out var normalized, out var error);

        Assert.True(success, error);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,hello")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData("ftp://example.com")]
    [InlineData("https://user:password@example.com")]
    public void TryNormalize_RejectsUnsafeOrUnsupportedUrls(string input)
    {
        Assert.False(UrlPolicy.TryNormalize(input, out _, out _));
    }

    [Fact]
    public void TryNormalize_RejectsControlCharacters()
    {
        Assert.False(UrlPolicy.TryNormalize("https://example.com/\npath", out _, out _));
    }
}
