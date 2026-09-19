namespace MapleWindow.Core.Scheduler;

/// <summary>
/// One thing a phrase rule decided to say — not yet resolved to actual text (that happens later,
/// via Phrases.IPhraseRepository + Phrases.PhraseFormatter, after mute/once-daily filtering).
/// RelatedContentNames has one entry for a per-item message (e.g. "이 특정 퀘스트 안 했어요"),
/// or every item in the group for an aggregate message (e.g. "일일 퀘스트 전부 안 했어요") — used so
/// a group message can be suppressed when every item it's about is individually muted.
/// </summary>
public sealed record PhraseMessage(IReadOnlyList<string> RelatedContentNames, string TemplateKey, IReadOnlyDictionary<string, string> Tokens);
