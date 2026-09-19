namespace MapleWindow.Core.Updates;

public interface IUpdateChecker
{
    Task<GithubReleaseInfo?> GetLatestReleaseAsync(CancellationToken ct = default);
}
