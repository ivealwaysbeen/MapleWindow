using MapleWindow.Core.Nexon.Models;
using MapleWindow.Core.OcidResolution;
using MapleWindow.Core.Tests.TestDoubles;

namespace MapleWindow.Core.Tests.OcidResolution;

public class OcidResolverTests
{
    private static CharacterListResponse ListOf(params (string Ocid, string Name, string World)[] characters)
        => new()
        {
            AccountList =
            [
                new AccountEntry
                {
                    AccountId = "acc-1",
                    CharacterList = characters
                        .Select(c => new CharacterSummary { Ocid = c.Ocid, CharacterName = c.Name, WorldName = c.World })
                        .ToList(),
                },
            ],
        };

    [Fact]
    public async Task ResolveAsync_MatchByNameAndWorld_ReturnsFoundWithOcid()
    {
        var fake = new FakeNexonApiClient { CharacterListResponse = ListOf(("ocid-1", "용사one", "스카니아")) };
        var resolver = new OcidResolver(fake);

        var result = await resolver.ResolveAsync("key", "용사one", "스카니아");

        Assert.Equal(OcidResolutionStatus.Found, result.Status);
        Assert.Equal("ocid-1", result.Ocid);
    }

    [Fact]
    public async Task ResolveAsync_NoMatchingCharacter_ReturnsNotFound()
    {
        var fake = new FakeNexonApiClient { CharacterListResponse = ListOf(("ocid-1", "용사one", "스카니아")) };
        var resolver = new OcidResolver(fake);

        var result = await resolver.ResolveAsync("key", "삭제된캐릭터", "스카니아");

        Assert.Equal(OcidResolutionStatus.NotFound, result.Status);
        Assert.Null(result.Ocid);
    }

    [Fact]
    public async Task ResolveAsync_CharacterOnDifferentAccount_StillMatches()
    {
        var list = new CharacterListResponse
        {
            AccountList =
            [
                new AccountEntry { AccountId = "acc-1", CharacterList = [new CharacterSummary { Ocid = "ocid-a", CharacterName = "다른캐릭", WorldName = "베라" }] },
                new AccountEntry { AccountId = "acc-2", CharacterList = [new CharacterSummary { Ocid = "ocid-b", CharacterName = "용사one", WorldName = "스카니아" }] },
            ],
        };
        var resolver = new OcidResolver(new FakeNexonApiClient { CharacterListResponse = list });

        var result = await resolver.ResolveAsync("key", "용사one", "스카니아");

        Assert.Equal(OcidResolutionStatus.Found, result.Status);
        Assert.Equal("ocid-b", result.Ocid);
    }

    [Fact]
    public async Task ResolveAsync_SameNameDifferentWorld_DoesNotCrossMatch()
    {
        var fake = new FakeNexonApiClient { CharacterListResponse = ListOf(("ocid-1", "용사one", "베라")) };
        var resolver = new OcidResolver(fake);

        var result = await resolver.ResolveAsync("key", "용사one", "스카니아");

        Assert.Equal(OcidResolutionStatus.NotFound, result.Status);
    }
}
