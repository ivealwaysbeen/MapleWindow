using System.Text.Json;
using MapleWindow.Core.Nexon.Models;

namespace MapleWindow.Core.Tests.Nexon;

public class DeserializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    [Fact]
    public void CharacterListResponse_MultipleAccountsAndCharacters_DeserializesAll()
    {
        const string json = """
        {
          "account_list": [
            {
              "account_id": "acc-1",
              "character_list": [
                { "ocid": "ocid-1", "character_name": "용사one", "world_name": "스카니아", "character_class": "히어로", "character_level": 260 },
                { "ocid": "ocid-2", "character_name": "용사two", "world_name": "베라", "character_class": "나이트로드", "character_level": 250 }
              ]
            },
            {
              "account_id": "acc-2",
              "character_list": []
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<CharacterListResponse>(json, Options)!;

        Assert.Equal(2, result.AccountList.Count);
        Assert.Equal(2, result.AccountList[0].CharacterList.Count);
        Assert.Empty(result.AccountList[1].CharacterList);

        var first = result.AccountList[0].CharacterList[0];
        Assert.Equal("ocid-1", first.Ocid);
        Assert.Equal("용사one", first.CharacterName);
        Assert.Equal("스카니아", first.WorldName);
        Assert.Equal("히어로", first.CharacterClass);
        Assert.Equal(260, first.CharacterLevel);
    }

    [Fact]
    public void CharacterBasicResponse_ParsesCharacterImageAndOptionalGuildName()
    {
        const string json = """
        {
          "date": "2026-09-19T00:00+09:00",
          "character_name": "용사one",
          "world_name": "스카니아",
          "character_gender": "남",
          "character_class": "히어로",
          "character_class_level": "6",
          "character_level": 260,
          "character_exp": 12345,
          "character_exp_rate": "12.3",
          "character_guild_name": null,
          "character_image": "https://open.api.nexon.com/static/maplestory/character/look/ABCDEFG",
          "character_date_create": "2020-01-01T00:00+09:00",
          "access_flag": "true",
          "liberation_quest_clear": "2"
        }
        """;

        var result = JsonSerializer.Deserialize<CharacterBasicResponse>(json, Options)!;

        Assert.Equal("https://open.api.nexon.com/static/maplestory/character/look/ABCDEFG", result.CharacterImage);
        Assert.Null(result.CharacterGuildName);
        Assert.Equal("true", result.AccessFlag);
    }

    [Fact]
    public void SchedulerCharacterStateResponse_ParsesDailyWeeklyAndBossArrays()
    {
        const string json = """
        {
          "date": "2026-09-19",
          "character_name": "용사one",
          "world_name": "스카니아",
          "character_level": 260,
          "character_class": "히어로",
          "daily_contents": [
            { "content_name": "[일일 퀘스트] 카르시온 복구 지원", "type": "quest", "registration_flag": "true", "now_count": 0, "max_count": 1, "quest_state": "0" }
          ],
          "weekly_contents": [
            { "content_name": "몬스터파크 익스트림", "type": "contents", "registration_flag": "true", "now_count": 2, "max_count": 5, "quest_state": "0" }
          ],
          "boss_contents": [
            { "content_name": "듄켈", "difficulty": "노멀", "cycle": "bossWeekly", "list_order_no": 1, "registration_flag": "true", "complete_flag": "false" }
          ],
          "weekly_boss_clear_count": 3,
          "weekly_boss_clear_limit_count": 14
        }
        """;

        var result = JsonSerializer.Deserialize<SchedulerCharacterStateResponse>(json, Options)!;

        var dailyItem = Assert.Single(result.DailyContents);
        Assert.Equal("quest", dailyItem.Type);
        Assert.Equal("true", dailyItem.RegistrationFlag);

        var weeklyItem = Assert.Single(result.WeeklyContents);
        Assert.Equal("contents", weeklyItem.Type);
        Assert.Equal(2, weeklyItem.NowCount);
        Assert.Equal(5, weeklyItem.MaxCount);

        var bossItem = Assert.Single(result.BossContents);
        Assert.Equal("bossWeekly", bossItem.Cycle);
        Assert.Equal("false", bossItem.CompleteFlag);

        Assert.Equal(3, result.WeeklyBossClearCount);
        Assert.Equal(14, result.WeeklyBossClearLimitCount);
    }

    [Fact]
    public void SchedulerCharacterStateResponse_MissingArrays_DefaultToEmptyNotNull()
    {
        const string json = """{ "date": "2026-09-19", "character_name": "x", "world_name": "y", "character_level": 1, "character_class": "z" }""";

        var result = JsonSerializer.Deserialize<SchedulerCharacterStateResponse>(json, Options)!;

        Assert.Empty(result.DailyContents);
        Assert.Empty(result.WeeklyContents);
        Assert.Empty(result.BossContents);
    }

    [Fact]
    public void ApiErrorResponse_ParsesNameAndMessage()
    {
        const string json = """{ "error": { "name": "OPENAPI00007", "message": "API 호출량 초과" } }""";

        var result = JsonSerializer.Deserialize<ApiErrorResponse>(json, Options)!;

        Assert.Equal("OPENAPI00007", result.Error.Name);
        Assert.Equal("API 호출량 초과", result.Error.Message);
    }
}
