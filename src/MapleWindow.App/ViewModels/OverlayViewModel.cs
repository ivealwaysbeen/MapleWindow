using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MapleWindow.App.Services;
using MapleWindow.Core.Animation;
using MapleWindow.Core.ImageCache;
using MapleWindow.Core.Scheduler;

namespace MapleWindow.App.ViewModels;

public partial class OverlayViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan AnimationTickInterval = TimeSpan.FromMilliseconds(180);
    private static readonly TimeSpan SpeechDisplayDuration = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan InterLineDelay = TimeSpan.FromSeconds(1);

    private readonly ICharacterImageCache _imageCache;
    private readonly SpriteFrameProcessor _spriteFrameProcessor;
    private readonly CharacterAppearanceService _appearanceService;
    private readonly SpeakCycleService _speakCycle;
    private readonly Dispatcher _dispatcher;

    private readonly DispatcherTimer _animationTimer;
    private readonly DispatcherTimer _speechTimer;
    private readonly DispatcherTimer _speechGapTimer;
    private readonly Queue<ResolvedSpeech> _speechQueue = new();

    private CharacterAnimationStateMachine? _stateMachine;
    private bool _isTicking;
    private bool _isPlayingSpeech;

    [ObservableProperty]
    private ImageSource? _spriteImage;

    [ObservableProperty]
    private double _scaleX = 1;

    [ObservableProperty]
    private double _windowLeft;

    [ObservableProperty]
    private string? _bubbleText;

    [ObservableProperty]
    private IReadOnlyList<string> _bubbleEmphasisTerms = Array.Empty<string>();

    /// <summary>Hidden (not Collapsed) during the inter-line gap: it keeps the bubble row's layout space
    /// reserved so the Auto-height row doesn't shrink to 0 and back, which was moving/resizing the overlay
    /// window (and visibly flickering it) on every single line instead of just when speech is fully done.</summary>
    [ObservableProperty]
    private Visibility _bubbleVisibility = Visibility.Collapsed;

    public OverlayViewModel(
        ICharacterImageCache imageCache,
        SpriteFrameProcessor spriteFrameProcessor,
        CharacterAppearanceService appearanceService,
        SpeakCycleService speakCycle,
        Dispatcher dispatcher)
    {
        _imageCache = imageCache;
        _spriteFrameProcessor = spriteFrameProcessor;
        _appearanceService = appearanceService;
        _speakCycle = speakCycle;
        _dispatcher = dispatcher;

        _animationTimer = new DispatcherTimer { Interval = AnimationTickInterval };
        _animationTimer.Tick += OnAnimationTick;

        _speechTimer = new DispatcherTimer { Interval = SpeechDisplayDuration };
        _speechTimer.Tick += OnSpeechTimerTick;

        _speechGapTimer = new DispatcherTimer { Interval = InterLineDelay };
        _speechGapTimer.Tick += OnSpeechGapTimerTick;

        _speakCycle.QueueReady += OnSpeakQueueReady;
        _speakCycle.IntervalChanged += OnQueueResetRequested;
        _speakCycle.QueueResetRequested += OnQueueResetRequested;
    }

    /// <summary>Starts sprite animation once the overlay window knows the work-area bounds it can walk within.</summary>
    public void StartAnimation(double minX, double maxX)
    {
        _stateMachine = new CharacterAnimationStateMachine(minX, maxX);
        _animationTimer.Start();
    }

    private async void OnAnimationTick(object? sender, EventArgs e)
    {
        if (_isTicking || _stateMachine is null) return;
        var baseUrl = _appearanceService.CharacterImageBaseUrl;
        if (string.IsNullOrEmpty(baseUrl)) return;

        _isTicking = true;
        try
        {
            var frame = _stateMachine.Tick();
            WindowLeft = frame.X;
            // The unflipped character render faces left by default (confirmed live — the original
            // Right-unflipped/Left-flipped assumption showed the character facing away from its travel
            // direction), so Right is the flipped case here.
            ScaleX = frame.Facing == FacingDirection.Right ? -1 : 1;

            var bytes = await _imageCache.GetFrameAsync(baseUrl, frame.Action.ToActionCode(), frame.FrameIndex).ConfigureAwait(true);
            var trimmed = await Task.Run(() => _spriteFrameProcessor.Process(bytes)).ConfigureAwait(true);
            SpriteImage = ToBitmapImage(trimmed);
        }
        catch
        {
            // Transient download/decode failure: keep showing the last-good frame rather than blanking the sprite.
        }
        finally
        {
            _isTicking = false;
        }
    }

    private void OnSpeakQueueReady(object? sender, IReadOnlyList<ResolvedSpeech> queue)
    {
        _dispatcher.BeginInvoke(() =>
        {
            foreach (var item in queue) _speechQueue.Enqueue(item);
            if (!_isPlayingSpeech) PlayNextSpeech();
        });
    }

    private void PlayNextSpeech()
    {
        if (_speechQueue.Count == 0)
        {
            BubbleVisibility = Visibility.Collapsed;
            _isPlayingSpeech = false;
            return;
        }

        _isPlayingSpeech = true;
        var next = _speechQueue.Dequeue();
        BubbleText = next.Text;
        BubbleEmphasisTerms = next.EmphasisTerms;
        BubbleVisibility = Visibility.Visible;
        _speechTimer.Stop();
        _speechTimer.Start();
    }

    /// <summary>Hides the bubble (without collapsing its layout space — see BubbleVisibility) and waits
    /// InterLineDelay before showing the next line, so consecutive lines don't flash by unreadably fast.</summary>
    private void OnSpeechTimerTick(object? sender, EventArgs e)
    {
        _speechTimer.Stop();
        BubbleVisibility = Visibility.Hidden;
        _speechGapTimer.Stop();
        _speechGapTimer.Start();
    }

    private void OnSpeechGapTimerTick(object? sender, EventArgs e)
    {
        _speechGapTimer.Stop();
        PlayNextSpeech();
    }

    /// <summary>The interval changing mid-display, or a character switch, means the queue was built under
    /// stale state, so drop it and hide the current bubble rather than let it finish playing out.</summary>
    private void OnQueueResetRequested(object? sender, EventArgs e)
    {
        _dispatcher.BeginInvoke(() =>
        {
            _speechTimer.Stop();
            _speechGapTimer.Stop();
            _speechQueue.Clear();
            _isPlayingSpeech = false;
            BubbleVisibility = Visibility.Collapsed;
        });
    }

    private static BitmapImage ToBitmapImage(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public void Dispose()
    {
        _animationTimer.Stop();
        _animationTimer.Tick -= OnAnimationTick;
        _speechTimer.Stop();
        _speechTimer.Tick -= OnSpeechTimerTick;
        _speechGapTimer.Stop();
        _speechGapTimer.Tick -= OnSpeechGapTimerTick;
        _speakCycle.QueueReady -= OnSpeakQueueReady;
        _speakCycle.IntervalChanged -= OnQueueResetRequested;
        _speakCycle.QueueResetRequested -= OnQueueResetRequested;
    }
}
