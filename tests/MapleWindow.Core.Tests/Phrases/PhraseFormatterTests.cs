using MapleWindow.Core.Phrases;

namespace MapleWindow.Core.Tests.Phrases;

public class PhraseFormatterTests
{
    [Fact]
    public void Format_SingleToken_Substituted()
    {
        var result = PhraseFormatter.Format("용사님~ {content_name}를 아직 완료하지 못했어요.", new Dictionary<string, string> { ["content_name"] = "카르시온 복구 지원" });

        Assert.Equal("용사님~ 카르시온 복구 지원를 아직 완료하지 못했어요.", result);
    }

    [Fact]
    public void Format_MultipleTokens_AllSubstituted()
    {
        var tokens = new Dictionary<string, string> { ["content_name"] = "듄켈", ["difficulty"] = "노멀" };

        var result = PhraseFormatter.Format("{content_name} : {difficulty}를 아직 처치하지 못했어요.", tokens);

        Assert.Equal("듄켈 : 노멀를 아직 처치하지 못했어요.", result);
    }

    [Fact]
    public void Format_UnknownToken_PassesThroughLiterally()
    {
        var result = PhraseFormatter.Format("{unknown_token} 텍스트", new Dictionary<string, string>());

        Assert.Equal("{unknown_token} 텍스트", result);
    }

    [Fact]
    public void Format_NoTokensInTemplate_ReturnsUnchanged()
    {
        var result = PhraseFormatter.Format("용사님~ 오늘 아직 일일퀘스트를 진행하지 않으셨어요!", new Dictionary<string, string>());

        Assert.Equal("용사님~ 오늘 아직 일일퀘스트를 진행하지 않으셨어요!", result);
    }

    [Theory]
    [InlineData("듄켈", "듄켈을 처치했어요.")] // batchim -> 을
    [InlineData("루오", "루오를 처치했어요.")] // no batchim -> 를
    public void Format_JosaToken_PicksFormMatchingBatchim(string contentName, string expected)
    {
        var result = PhraseFormatter.Format(
            "{content_name:을/를} 처치했어요.",
            new Dictionary<string, string> { ["content_name"] = contentName });

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_JosaToken_UnknownToken_PassesThroughLiterally()
    {
        var result = PhraseFormatter.Format("{unknown_token:을/를} 처치했어요.", new Dictionary<string, string>());

        Assert.Equal("{unknown_token:을/를} 처치했어요.", result);
    }
}
