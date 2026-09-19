using MapleWindow.Core.Phrases;

namespace MapleWindow.Core.Tests.Phrases;

public class KoreanJosaTests
{
    [Theory]
    [InlineData("듄켈", "받침")] // 켈 ends in ㄹ
    [InlineData("핑크빈", "받침")] // 빈 ends in ㄴ
    [InlineData("루오", "무받침")] // 오 has no final consonant
    [InlineData("카르시온 복구 지원", "받침")] // last word ends in ㄴ
    public void Attach_HangulWord_PicksFormMatchingBatchim(string word, string expectedForm)
    {
        var result = KoreanJosa.Attach(word, withBatchim: "받침", withoutBatchim: "무받침");

        Assert.Equal(expectedForm, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Attach_EmptyOrNullWord_FallsBackToWithoutBatchim(string? word)
    {
        var result = KoreanJosa.Attach(word!, withBatchim: "받침", withoutBatchim: "무받침");

        Assert.Equal("무받침", result);
    }

    [Fact]
    public void Attach_NonHangulWord_FallsBackToWithoutBatchim()
    {
        var result = KoreanJosa.Attach("EXTREME", withBatchim: "받침", withoutBatchim: "무받침");

        Assert.Equal("무받침", result);
    }
}
