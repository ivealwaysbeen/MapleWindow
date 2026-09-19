namespace MapleWindow.Core.Animation;

/// <summary>Only A00 (stand) and A02 (walk) — A01/A03 dropped: Nexon's static look renderer doesn't reliably
/// apply the wmotion override to those two (confirmed live, same equip/wmotion, wrong weapon grip came back
/// intermittently).</summary>
public enum SpriteAction { Stand, Walk }

public enum FacingDirection { Right, Left }

public readonly record struct AnimationFrame(SpriteAction Action, int FrameIndex, FacingDirection Facing, double X);

public static class SpriteActionExtensions
{
    public static string ToActionCode(this SpriteAction action) => action switch
    {
        SpriteAction.Stand => "A00",
        SpriteAction.Walk => "A02",
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    public static int FrameCount(this SpriteAction action) => action switch
    {
        SpriteAction.Stand => 3,
        SpriteAction.Walk => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };
}
