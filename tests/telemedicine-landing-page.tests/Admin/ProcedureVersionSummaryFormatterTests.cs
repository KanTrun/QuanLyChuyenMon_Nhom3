using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Tests.Admin;

public sealed class ProcedureVersionSummaryFormatterTests
{
    [Fact]
    public void Display_ParsesNoteFromJson()
    {
        var display = ProcedureVersionSummaryFormatter.Display("{\"note\":\"Mô tả phiên bản\"}");
        Assert.Equal("Mô tả phiên bản", display);
    }

    [Fact]
    public void Display_ReturnsPlainTextWhenNotJson()
    {
        Assert.Equal("Tóm tắt thường", ProcedureVersionSummaryFormatter.Display("Tóm tắt thường"));
    }

    [Fact]
    public void ToStorageJson_WrapsNote()
    {
        var json = ProcedureVersionSummaryFormatter.ToStorageJson("Ban hành lần 2");
        Assert.StartsWith("{", json);
        Assert.Equal("Ban hành lần 2", ProcedureVersionSummaryFormatter.Display(json));
    }
}
