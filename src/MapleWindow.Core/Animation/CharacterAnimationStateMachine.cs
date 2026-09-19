namespace MapleWindow.Core.Animation;

/// <summary>
/// Pure logic, no WPF types — Idle (A00, 4-12s dwell) alternates with Walking (A02, 80-320px random
/// move), driven by an injectable clock/RNG so it's deterministically testable. The caller (OverlayWindow's
/// DispatcherTimer) calls Tick() roughly every WalkFrameInterval and applies the result to the actual window.
/// Idle's frame index ping-pongs (0,1,2,1,0,...) rather than looping, matching how the stand sprite reads.
/// </summary>
public sealed class CharacterAnimationStateMachine
{
    private const double WalkSpeedPxPerSec = 40;
    private static readonly TimeSpan StandFrameInterval = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan WalkFrameInterval = TimeSpan.FromMilliseconds(180);

    private enum AnimState { Idle, Walking }

    private readonly Random _rng;
    private readonly Func<DateTime> _clock;
    private readonly double _minX;
    private readonly double _maxX;

    private AnimState _state;
    private SpriteAction _action;
    private int _frameIndex;
    private int _frameTick;
    private double _x;
    private double _walkTargetX;
    private FacingDirection _facing = FacingDirection.Right;
    private DateTime _stateEndsAt;
    private DateTime _lastFrameAt;

    public CharacterAnimationStateMachine(double minX, double maxX, int? seed = null, Func<DateTime>? clock = null)
    {
        _rng = seed is { } s ? new Random(s) : new Random();
        _clock = clock ?? (() => DateTime.UtcNow);
        _minX = minX;
        _maxX = maxX;
        _x = (minX + maxX) / 2;

        var now = _clock();
        _lastFrameAt = now;
        EnterIdle(now);
    }

    public AnimationFrame Tick()
    {
        var now = _clock();
        var frameInterval = _state == AnimState.Idle ? StandFrameInterval : WalkFrameInterval;

        // Idle ping-pongs its frames (0,1,2,1,0,...); Walking loops its step cycle and also moves X.
        if (now - _lastFrameAt >= frameInterval)
        {
            _lastFrameAt = now;
            _frameTick++;
            _frameIndex = _state == AnimState.Idle
                ? PingPongIndex(_frameTick, _action.FrameCount())
                : _frameTick % _action.FrameCount();
            if (_state == AnimState.Walking) StepPosition();
        }

        if (now >= _stateEndsAt)
        {
            if (_state == AnimState.Idle) EnterWalking(now); else EnterIdle(now);
        }

        return new AnimationFrame(_action, _frameIndex, _facing, _x);
    }

    private static int PingPongIndex(int tick, int frameCount)
    {
        if (frameCount <= 1) return 0;
        var cycle = 2 * (frameCount - 1);
        var pos = tick % cycle;
        return pos < frameCount ? pos : cycle - pos;
    }

    private void EnterIdle(DateTime now)
    {
        _state = AnimState.Idle;
        _action = SpriteAction.Stand;
        _frameIndex = 0;
        _frameTick = 0;
        _stateEndsAt = now + TimeSpan.FromSeconds(_rng.Next(4, 13));
    }

    private void EnterWalking(DateTime now)
    {
        _state = AnimState.Walking;
        _action = SpriteAction.Walk;
        _frameIndex = 0;
        _frameTick = 0;

        var distance = _rng.Next(80, 321);
        var target = Math.Clamp(_rng.NextDouble() < 0.5 ? _x - distance : _x + distance, _minX, _maxX);
        _facing = target >= _x ? FacingDirection.Right : FacingDirection.Left;
        _walkTargetX = target;

        var seconds = Math.Max(1.5, Math.Abs(target - _x) / WalkSpeedPxPerSec);
        _stateEndsAt = now + TimeSpan.FromSeconds(seconds);
    }

    private void StepPosition()
    {
        var remaining = _walkTargetX - _x;
        var step = WalkSpeedPxPerSec * WalkFrameInterval.TotalSeconds;
        _x = Math.Abs(remaining) <= step ? _walkTargetX : _x + Math.Sign(remaining) * step;
    }
}
