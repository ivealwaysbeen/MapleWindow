using MapleWindow.Core.Phrases;

namespace MapleWindow.Core.Tests.Phrases;

public class PhraseRepositoryTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"maplewindow-test-phrases-{Guid.NewGuid():N}.json");

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    [Fact]
    public void FirstConstruction_NoExistingFile_SeedsFromEmbeddedDefaults()
    {
        Assert.False(File.Exists(_tempPath));

        var repository = new PhraseRepository(_tempPath);

        Assert.True(File.Exists(_tempPath));
        Assert.NotNull(repository.GetRandomPhrase("daily.quest.allUndone"));
    }

    [Fact]
    public void GetRandomPhrase_KnownKey_ReturnsOneOfTheCandidates()
    {
        var repository = new PhraseRepository(_tempPath);

        var phrase = repository.GetRandomPhrase("daily.monsterPark.notStarted");

        Assert.Equal("용사님~ 아직 몬스터 파크를 진행하지 않으셨어요!", phrase);
    }

    [Fact]
    public void GetRandomPhrase_UnknownKey_ReturnsNull()
    {
        var repository = new PhraseRepository(_tempPath);

        Assert.Null(repository.GetRandomPhrase("does.not.exist"));
    }

    [Fact]
    public void Reload_PicksUpManualEditsFromDisk()
    {
        var repository = new PhraseRepository(_tempPath);
        Assert.Null(repository.GetRandomPhrase("custom.key"));

        File.WriteAllText(_tempPath, """{ "entries": { "custom.key": ["수동으로 추가한 멘트"] } }""");
        repository.Reload();

        Assert.Equal("수동으로 추가한 멘트", repository.GetRandomPhrase("custom.key"));
    }
}
