using AppWeb.Core.Models;
using AppWeb.Core.Validation;

namespace AppWeb.Tests;

public sealed class WebAppValidatorTests
{
    [Fact]
    public void Normalize_TrimsFieldsAndUrl()
    {
        var draft = new WebAppDraft(
            "  Notion  ",
            "  Notas  ",
            "notion.so",
            BrowserIds.WebView2,
            false,
            false);

        var result = WebAppValidator.Normalize(draft);

        Assert.Equal("Notion", result.Name);
        Assert.Equal("Notas", result.Description);
        Assert.Equal("https://notion.so/", result.Url);
    }

    [Fact]
    public void Normalize_RejectsEmptyName()
    {
        var draft = new WebAppDraft(" ", null, "https://example.com", BrowserIds.WebView2, false, false);

        Assert.Throws<ValidationException>(() => WebAppValidator.Normalize(draft));
    }

    [Fact]
    public void Normalize_RejectsLongDescription()
    {
        var draft = new WebAppDraft(
            "App",
            new string('x', 501),
            "https://example.com",
            BrowserIds.WebView2,
            false,
            false);

        Assert.Throws<ValidationException>(() => WebAppValidator.Normalize(draft));
    }
}
