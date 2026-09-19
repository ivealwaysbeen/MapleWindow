using MapleWindow.Core.Config;
using MapleWindow.Core.Nexon.Models;
using MapleWindow.Core.Scheduler;
using MapleWindow.Core.Tests.TestDoubles;

namespace MapleWindow.Core.Tests.Scheduler;

public class SchedulerPollingServiceTests
{
    private static AppConfig SomeConfig() => new() { ApiKey = "key", Ocid = "ocid-1", CharacterName = "용사one", WorldName = "스카니아" };

    [Fact]
    public async Task PollOnceAsync_NoConfigYet_DoesNothing_NoException()
    {
        var api = new FakeNexonApiClient();
        var service = new SchedulerPollingService(api, new FakeConfigStore(initial: null), new FakePhraseRepository());

        await service.PollOnceAsync();

        Assert.Equal(0, api.GetSchedulerStateCallCount);
        Assert.Empty(service.CurrentPool.DailyContents);
    }

    [Fact]
    public async Task PollOnceAsync_Success_UpdatesCurrentPool_RaisesPoolUpdated()
    {
        var api = new FakeNexonApiClient
        {
            SchedulerResponse = new SchedulerCharacterStateResponse
            {
                DailyContents = [new DailyWeeklyContentItem { ContentName = "d", Type = "quest", RegistrationFlag = "true", QuestState = "0" }],
            },
        };
        var service = new SchedulerPollingService(api, new FakeConfigStore(SomeConfig()), new FakePhraseRepository());
        ScheduledContentPool? raised = null;
        service.PoolUpdated += (_, pool) => raised = pool;

        await service.PollOnceAsync();

        Assert.Single(service.CurrentPool.DailyContents);
        Assert.NotNull(raised);
        Assert.Single(raised!.DailyContents);
    }

    [Fact]
    public async Task PollOnceAsync_ApiThrows_KeepsLastGoodPool_RaisesPollFailed()
    {
        var api = new FakeNexonApiClient
        {
            SchedulerResponse = new SchedulerCharacterStateResponse
            {
                DailyContents = [new DailyWeeklyContentItem { ContentName = "d", Type = "quest", RegistrationFlag = "true", QuestState = "0" }],
            },
        };
        var service = new SchedulerPollingService(api, new FakeConfigStore(SomeConfig()), new FakePhraseRepository());
        await service.PollOnceAsync(); // first, successful poll establishes a "last good" pool
        Assert.Single(service.CurrentPool.DailyContents);

        api.SchedulerException = new InvalidOperationException("network down");
        Exception? raised = null;
        service.PollFailed += (_, ex) => raised = ex;

        await service.PollOnceAsync();

        Assert.NotNull(raised);
        Assert.Single(service.CurrentPool.DailyContents); // unchanged, not cleared
    }

    [Fact]
    public async Task PollOnceAsync_ReloadsPhraseRepositoryEachTick()
    {
        var api = new FakeNexonApiClient();
        var phrases = new FakePhraseRepository();
        var service = new SchedulerPollingService(api, new FakeConfigStore(SomeConfig()), phrases);

        await service.PollOnceAsync();
        await service.PollOnceAsync();

        Assert.Equal(2, phrases.ReloadCallCount);
    }
}
