namespace MapleWindow.Core.Scheduler.PhraseRules;

public interface IPhraseRule
{
    IEnumerable<PhraseMessage> Evaluate(ScheduledContentPool pool, DateTime now);
}
