using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using FSpot.Avalonia.Controls;
using FSpot.Avalonia.Imaging;
using FSpot.Avalonia.Input;
using FSpot.Avalonia.Services;
using FSpot.Avalonia.ViewModels;
using FSpot.Resources.Lang;
using FSpot.Slideshow;

namespace FSpot.Avalonia;

/// <summary>
/// The GTK FullScreenView: a still view of the collection that can play a slideshow with transitions,
/// under a fading control overlay.
/// </summary>
public partial class SlideshowWindow : Window
{
	static readonly Cursor HiddenCursor = new (StandardCursorType.None);

	readonly SlideshowSequence sequence;
	readonly IFullScreenHost? host;
	readonly Func<string, int, Bitmap?> decodeSlide;
	readonly PhotoInfoViewModel info = new ();
	readonly DispatcherTimer flipTimer;
	readonly DispatcherTimer animationTimer;
	readonly DispatcherTimer cursorTimer;
	readonly DispatcherTimer hideControlsTimer;
	readonly DispatcherTimer fadeTimer;
	readonly Stopwatch animationClock = new ();
	Bitmap? slide;
	Bitmap? previousSlide;
	int slideLoad;
	bool slidePending;
	CancellationTokenSource slideDecode = new ();
	bool playing;
	bool busy;
	SlideshowTransitionKind transition = SlideshowTransition.All[0];
	ControlOverlayVisibility controlsVisibility;
	double controlsTarget;
	DateTime dismissedUntil;

	public SlideshowWindow () : this (new[] { "about:blank" }, 0)
	{
	}

	public SlideshowWindow (IReadOnlyList<string> paths, int startIndex, IFullScreenHost? host = null, bool play = true, Func<string, int, Bitmap?>? decode = null)
	{
		sequence = new SlideshowSequence (paths, startIndex);
		this.host = host;
		decodeSlide = decode ?? ((path, size) => AvaloniaPhotoDecoder.DecodeFile (path, size));
		InitializeComponent ();

		InfoOverlay.DataContext = info;
		// PhotoPopup reads the main window model from this window when the menu opens.
		if (host is MainWindowViewModel viewModel)
			DataContext = viewModel;
		TransitionCombo.ItemsSource = SlideshowTransition.All.Select (TransitionName).ToList ();
		TransitionCombo.SelectedIndex = 0;

		flipTimer = new DispatcherTimer { Interval = SlideshowTransition.Interval };
		flipTimer.Tick += (_, _) => Flip ();
		animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds (16) };
		animationTimer.Tick += (_, _) => StepAnimation ();
		cursorTimer = new DispatcherTimer { Interval = ControlOverlayPolicy.CursorHide };
		cursorTimer.Tick += (_, _) => HideCursor ();
		hideControlsTimer = new DispatcherTimer { Interval = ControlOverlayPolicy.AutoHide };
		hideControlsTimer.Tick += (_, _) => AutoHideControls ();
		fadeTimer = new DispatcherTimer { Interval = ControlOverlayPolicy.FadeTick };
		fadeTimer.Tick += (_, _) => StepFade ();

		AddHandler (KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
		AddHandler (PointerMovedEvent, OnAnyPointerMoved, RoutingStrategies.Tunnel);

		Opened += (_, _) => {
			ShowCurrent (animate: false);
			DismissControls ();
			cursorTimer.Start ();
			if (play)
				PlayPause ();
			Focus ();
		};
		Closed += (_, _) => {
			slideLoad++;
			slideDecode.Cancel ();
			flipTimer.Stop ();
			animationTimer.Stop ();
			cursorTimer.Stop ();
			hideControlsTimer.Stop ();
			fadeTimer.Stop ();
			if (sequence.Count > 0)
				host?.FocusPhoto (sequence.Current);
			sequence.Stop ();
			Slide.Source = null;
			PreviousSlide.Source = null;
			ReleasedBitmap.DisposePrevious (previousSlide, null);
			ReleasedBitmap.DisposePrevious (slide, null);
			previousSlide = slide = null;
		};
	}

	public bool IsPlaying => playing;

	// A tick while a decode is in flight would advance as soon as that decode finishes.
	internal bool FlipIsScheduled => flipTimer.IsEnabled;

	public string CurrentPath => sequence.Current;

	static string TransitionName (SlideshowTransitionKind kind)
		=> kind switch {
			SlideshowTransitionKind.Cover => Strings.Cover,
			SlideshowTransitionKind.Dissolve => Strings.Dissolve,
			SlideshowTransitionKind.Push => Strings.Push,
			_ => kind.ToString (),
		};

	public void PlayPause ()
	{
		if (busy)
			return;
		playing = !playing;
		PlayPauseGlyph.Text = playing ? "❚❚" : "▶";
		if (playing) {
			// The interval starts when the slide is visible, not when the decode is queued.
			if (!slidePending)
				flipTimer.Start ();
		} else {
			flipTimer.Stop ();
			FinishAnimation ();
		}
	}

	// The flip timer. A tick during a decode would discard the slide that is about to appear.
	internal void Flip ()
	{
		if (slidePending)
			return;
		Go (1);
	}

	void Go (int delta)
	{
		if (busy)
			return;
		bool moved = delta > 0 ? sequence.Advance () : sequence.Retreat ();
		if (!moved)
			return;
		if (playing)
			flipTimer.Stop ();
		ShowCurrent (animate: playing);
	}

	void ShowCurrent (bool animate)
		=> _ = ShowCurrentAsync (animate);

	async Task ShowCurrentAsync (bool animate)
	{
		string path = sequence.Current;
		int generation = ++slideLoad;
		var token = RenewSlideDecode ();
		slidePending = true;
		flipTimer.Stop ();
		Bitmap? next;
		try {
			next = await DecodeGate.RunAsync (
				() => decodeSlide (path, EditorPreview.MaxPixels),
				token);
		} catch {
			next = null;
		}

		if (generation != slideLoad) {
			next?.Dispose ();
			return;
		}

		slidePending = false;
		if (playing)
			flipTimer.Start ();

		if (animate) {
			var discarded = previousSlide;
			previousSlide = slide;
			PreviousSlide.Source = previousSlide;
			ReleasedBitmap.DisposePrevious (discarded, previousSlide);
			slide = next;
			Slide.Source = next;
			animationClock.Restart ();
			ApplyFrame (SlideshowTransition.Frame (transition, 0));
			animationTimer.Start ();
		} else {
			var old = slide;
			slide = next;
			Slide.Source = next;
			ReleasedBitmap.DisposePrevious (old, next);
			FinishAnimation ();
		}

		UpdateInfo ();
	}

	// The previous slide is still waiting on the one decode gate. Cancelling it means
	// that wait never starts the decode.
	CancellationToken RenewSlideDecode ()
	{
		var previous = slideDecode;
		slideDecode = new CancellationTokenSource ();
		previous.Cancel ();
		previous.Dispose ();
		return slideDecode.Token;
	}

	void StepAnimation ()
	{
		double progress = SlideshowTransition.Progress (animationClock.Elapsed);
		ApplyFrame (SlideshowTransition.Frame (transition, progress));
		if (progress >= 1)
			FinishAnimation ();
	}

	void FinishAnimation ()
	{
		animationTimer.Stop ();
		animationClock.Reset ();
		ApplyFrame (SlideshowTransition.Frame (transition, 1));
		PreviousLayer.IsVisible = false;
		PreviousSlide.Source = null;
		ReleasedBitmap.DisposePrevious (previousSlide, slide);
		previousSlide = null;
	}

	void ApplyFrame (SlideshowFrame frame)
	{
		double width = Stage.Bounds.Width;
		PreviousLayer.IsVisible = previousSlide is not null;
		PreviousLayer.Opacity = frame.PreviousOpacity;
		NextLayer.Opacity = frame.NextOpacity;
		if (PreviousLayer.RenderTransform is TranslateTransform previous)
			previous.X = frame.PreviousOffset * width;
		if (NextLayer.RenderTransform is TranslateTransform next)
			next.X = frame.NextOffset * width;
	}

	void UpdateInfo ()
	{
		if (!InfoOverlay.IsVisible || sequence.Count == 0)
			return;
		if (host?.PhotoAt (sequence.Current) is { } photo) {
			info.Update (photo);
			return;
		}
		info.Clear ();
		info.Name = Path.GetFileName (sequence.Current);
	}

	void ToggleInfo ()
	{
		bool show = !InfoOverlay.IsVisible;
		InfoOverlay.IsVisible = show;
		InfoButton.IsChecked = show;
		UpdateInfo ();
	}

	void Rotate (bool clockwise)
	{
		if (host is null || sequence.Count == 0)
			return;
		host.RotatePhoto (sequence.Current, clockwise);
		ShowCurrent (animate: false);
	}

	void Rate (uint rating)
	{
		if (host is null || sequence.Count == 0)
			return;
		host.RatePhoto (sequence.Current, rating);
		UpdateInfo ();
	}

	async Task RemoveAsync (bool deleteFromDrive)
	{
		if (host is null || busy || sequence.Count == 0)
			return;
		string path = sequence.Current;
		busy = true;
		bool resume = playing;
		bool showedNext = false;
		flipTimer.Stop ();
		try {
			bool removed = await host.RemovePhotoAsync (path, deleteFromDrive);
			if (!IsVisible)
				return;
			if (!removed)
				return;
			if (!sequence.Remove (path)) {
				Close ();
				return;
			}
			_ = ShowCurrentAsync (animate: false);
			showedNext = true;
		} finally {
			busy = false;
			if (resume && playing && IsVisible && !showedNext && !slidePending)
				flipTimer.Start ();
			// GTK presents the fullscreen view again so the confirmation dialog does not leave it buried.
			if (IsVisible)
				Activate ();
		}
	}

	void Execute (FullScreenKeyAction action)
	{
		switch (action.Command) {
		case FullScreenCommand.Exit:
			Close ();
			break;
		case FullScreenCommand.ToggleInfo:
			ToggleInfo ();
			break;
		case FullScreenCommand.RotateLeft:
			Rotate (clockwise: false);
			break;
		case FullScreenCommand.RotateRight:
			Rotate (clockwise: true);
			break;
		case FullScreenCommand.Rate:
			Rate (action.Rating);
			break;
		case FullScreenCommand.RemoveFromCatalog:
			_ = RemoveAsync (deleteFromDrive: false);
			break;
		case FullScreenCommand.DeleteFromDrive:
			_ = RemoveAsync (deleteFromDrive: true);
			break;
		case FullScreenCommand.Next:
			Go (1);
			break;
		case FullScreenCommand.Previous:
			Go (-1);
			break;
		}
	}

	void OnPreviewKeyDown (object? sender, KeyEventArgs e)
	{
		if (TransitionCombo.IsDropDownOpen)
			return;
		var action = FullScreenKeys.Map (e.Key, e.KeyModifiers);
		if (action.Command == FullScreenCommand.None)
			return;
		// F, Q, F11 and Escape leave the show even when a combo or text box is focused.
		var focused = FocusManager?.GetFocusedElement ();
		if (action.Command != FullScreenCommand.Exit
			&& !Input.ShortcutGuards.AllowsUnadornedKey (focused, e.Key))
			return;
		e.Handled = true;
		Execute (action);
	}

	void OnAnyPointerMoved (object? sender, PointerEventArgs e)
	{
		Cursor = Cursor.Default;
		cursorTimer.Stop ();
		cursorTimer.Start ();

		if (ControlBar.IsVisible && ControlBar.IsPointerOver) {
			SetControlsVisibility (ControlOverlayVisibility.Full);
			return;
		}
		if (ControlOverlayPolicy.RevealsControls (e.GetPosition (this).Y, Bounds.Height))
			SetControlsVisibility (ControlOverlayVisibility.Partial);
	}

	void HideCursor ()
	{
		cursorTimer.Stop ();
		Cursor = HiddenCursor;
	}

	void SetControlsVisibility (ControlOverlayVisibility visibility)
	{
		if (visibility != ControlOverlayVisibility.None && DateTime.UtcNow < dismissedUntil)
			return;
		controlsVisibility = visibility;
		controlsTarget = ControlOverlayPolicy.TargetOpacity (visibility);
		fadeTimer.Start ();
		if (controlsTarget > 0) {
			hideControlsTimer.Stop ();
			hideControlsTimer.Start ();
		}
	}

	void DismissControls ()
	{
		SetControlsVisibility (ControlOverlayVisibility.None);
		dismissedUntil = DateTime.UtcNow + ControlOverlayPolicy.DismissGrace;
	}

	void AutoHideControls ()
	{
		if (ControlBar.IsPointerOver)
			return;
		hideControlsTimer.Stop ();
		SetControlsVisibility (ControlOverlayVisibility.None);
	}

	void StepFade ()
	{
		double opacity = ControlOverlayPolicy.StepToward (ControlBar.Opacity, controlsTarget);
		ControlBar.Opacity = opacity;
		ControlBar.IsVisible = opacity > 0.05 || controlsVisibility != ControlOverlayVisibility.None;
		if (opacity == controlsTarget) {
			fadeTimer.Stop ();
			ControlBar.IsVisible = opacity > 0.05;
		}
	}

	void OnExitClick (object? sender, RoutedEventArgs e)
		=> Close ();

	void OnPreviousClick (object? sender, RoutedEventArgs e)
		=> Go (-1);

	void OnNextClick (object? sender, RoutedEventArgs e)
		=> Go (1);

	void OnPlayPauseClick (object? sender, RoutedEventArgs e)
		=> PlayPause ();

	void OnRotateLeftClick (object? sender, RoutedEventArgs e)
		=> Rotate (clockwise: false);

	void OnRotateRightClick (object? sender, RoutedEventArgs e)
		=> Rotate (clockwise: true);

	void OnInfoClick (object? sender, RoutedEventArgs e)
	{
		// The ToggleButton already flipped itself; keep the overlay in step with it.
		InfoOverlay.IsVisible = InfoButton.IsChecked == true;
		UpdateInfo ();
	}

	void OnHideClick (object? sender, RoutedEventArgs e)
		=> DismissControls ();

	void OnTransitionChanged (object? sender, SelectionChangedEventArgs e)
	{
		int index = TransitionCombo.SelectedIndex;
		if (index >= 0 && index < SlideshowTransition.All.Count)
			transition = SlideshowTransition.All[index];
	}
}
