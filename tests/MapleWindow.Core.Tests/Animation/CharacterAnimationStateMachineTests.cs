using MapleWindow.Core.Animation;

namespace MapleWindow.Core.Tests.Animation;

public class CharacterAnimationStateMachineTests
{
    private static (CharacterAnimationStateMachine Machine, Func<DateTime> Advance) Build(double minX, double maxX, int seed)
    {
        var now = new DateTime(2026, 1, 1);
        var machine = new CharacterAnimationStateMachine(minX, maxX, seed, () => now);
        return (machine, () => now = now.AddMilliseconds(180));
    }

    [Fact]
    public void Tick_ManyIterations_XAlwaysWithinBounds()
    {
        var (machine, advance) = Build(minX: 0, maxX: 1000, seed: 42);

        for (var i = 0; i < 5000; i++)
        {
            advance();
            var frame = machine.Tick();
            Assert.InRange(frame.X, 0, 1000);
        }
    }

    [Fact]
    public void Tick_ManyIterations_FrameIndexAlwaysValidForCurrentAction()
    {
        var (machine, advance) = Build(minX: 0, maxX: 1000, seed: 123);

        for (var i = 0; i < 5000; i++)
        {
            advance();
            var frame = machine.Tick();
            Assert.InRange(frame.FrameIndex, 0, frame.Action.FrameCount() - 1);
        }
    }

    [Fact]
    public void Tick_ManyIterations_VisitsBothIdleAndWalkingActions()
    {
        var (machine, advance) = Build(minX: 0, maxX: 1000, seed: 7);
        var actionsSeen = new HashSet<SpriteAction>();

        for (var i = 0; i < 5000; i++)
        {
            advance();
            actionsSeen.Add(machine.Tick().Action);
        }

        Assert.Contains(SpriteAction.Stand, actionsSeen);
        Assert.Contains(SpriteAction.Walk, actionsSeen);
    }

    [Fact]
    public void Tick_ManyIterations_NeverUsesBuggyA01OrA03Frames()
    {
        // Nexon's static look renderer doesn't reliably apply the wmotion override to A01/A03 (confirmed
        // live: same wmotion, same equip, but A01 frames intermittently rendered the wrong weapon grip) —
        // A00 (stand) / A02 (walk) are the only actions requested now.
        var (machine, advance) = Build(minX: 0, maxX: 1000, seed: 13);

        for (var i = 0; i < 5000; i++)
        {
            advance();
            var action = machine.Tick().Action;
            Assert.True(action is SpriteAction.Stand or SpriteAction.Walk);
        }
    }

    [Fact]
    public void WhileStanding_XNeverChanges_ButFrameIndexLoops()
    {
        var (machine, advance) = Build(minX: 0, maxX: 1000, seed: 55);

        // The constructor always enters Idle, so this first tick (180ms in) is guaranteed to still be
        // standing — the minimum idle dwell is 4s, far longer than the ticks this test performs.
        advance();
        var first = machine.Tick();
        Assert.Equal(SpriteAction.Stand, first.Action);

        var frameIndexesSeen = new HashSet<int> { first.FrameIndex };
        for (var i = 0; i < 10; i++)
        {
            advance();
            var frame = machine.Tick();
            Assert.Equal(first.Action, frame.Action);
            Assert.Equal(first.X, frame.X);
            Assert.InRange(frame.FrameIndex, 0, frame.Action.FrameCount() - 1);
            frameIndexesSeen.Add(frame.FrameIndex);
        }

        Assert.True(frameIndexesSeen.Count > 1, "expected the idle loop to visit more than one frame");
    }

    [Fact]
    public void Tick_NarrowBounds_NeverExceedsBounds()
    {
        // Degenerate-ish range still shouldn't throw or escape bounds.
        var (machine, advance) = Build(minX: 100, maxX: 120, seed: 99);

        for (var i = 0; i < 2000; i++)
        {
            advance();
            var frame = machine.Tick();
            Assert.InRange(frame.X, 100, 120);
        }
    }
}
