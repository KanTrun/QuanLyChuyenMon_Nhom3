using TelemedicineLandingPage.Services.Chatbot;

namespace telemedicine_landing_page.tests.Chatbot;

public class MiniMarkdownTests
{
    [Fact]
    public void EscapesHtmlSpecialCharacters()
    {
        var html = MiniMarkdown.ToHtml("a < b & c > d \"quote\" 'apos'");

        Assert.Contains("&lt;", html);
        Assert.Contains("&gt;", html);
        Assert.Contains("&amp;", html);
        Assert.Contains("&quot;", html);
        Assert.DoesNotContain("<b>", html);
    }

    [Fact]
    public void AppliesBoldItalicCode()
    {
        var html = MiniMarkdown.ToHtml("Đây là **đậm** và *nghiêng* và `code`.");

        Assert.Contains("<strong>đậm</strong>", html);
        Assert.Contains("<em>nghiêng</em>", html);
        Assert.Contains("<code>code</code>", html);
    }

    [Fact]
    public void BulletsRenderAsList()
    {
        var html = MiniMarkdown.ToHtml("Các bước:\n- Bước một\n- Bước hai\n- Bước ba");

        Assert.Contains("<ul>", html);
        Assert.Contains("</ul>", html);
        Assert.Contains("<li>Bước một</li>", html);
        Assert.Contains("<li>Bước hai</li>", html);
        Assert.Contains("<li>Bước ba</li>", html);
    }

    [Fact]
    public void NewlinesBecomeBr()
    {
        var html = MiniMarkdown.ToHtml("Dòng một\nDòng hai");

        Assert.Contains("<br/>", html);
        Assert.Contains("Dòng một", html);
        Assert.Contains("Dòng hai", html);
    }

    [Fact]
    public void MaliciousScriptIsEscaped()
    {
        var html = MiniMarkdown.ToHtml("<script>alert('xss')</script>");

        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("</script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void EmptyInputReturnsEmptyString()
    {
        Assert.Equal(string.Empty, MiniMarkdown.ToHtml(string.Empty));
        Assert.Equal(string.Empty, MiniMarkdown.ToHtml(null));
    }
}
