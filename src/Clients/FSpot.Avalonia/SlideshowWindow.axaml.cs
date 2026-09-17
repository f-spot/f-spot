using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using FSpot.Slideshow;

namespace FSpot.Avalonia;

public partial class SlideshowWindow : Window
{
	readonly SlideshowSequence sequence;
	readonly DispatcherTimer timer;

	public SlideshowWindow () : this (new[] { "about:blank" }, 0)
	{
	}

	public SlideshowWindow (IReadOnlyList<string> paths, int startIndex)
	{
		sequence = new SlideshowSequence (paths, startIndex);
		InitializeComponent ();
		timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds (3) };
		timer.Tick += (_, _) => {
			if (!sequence.Advance ())
				return;
			ShowCurrent ();
		};
		Opened += (_, _) => {
			ShowCurrent ();
			timer.Start ();
		};
		Closed += (_, _) => {
			sequence.Stop ();
			timer.Stop ();
		};
	}

	void ShowCurrent ()
	{
		try {
			Slide.Source = new Bitmap (sequence.Current);
		} catch {
			Slide.Source = null;
		}
	}

	void OnCloseClick (object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
		=> Close ();

	protected override void OnKeyDown (KeyEventArgs e)
	{
		if (e.Key == Key.Escape || e.Key == Key.Q) {
			sequence.Stop ();
			Close ();
			e.Handled = true;
			return;
		}
		if (e.Key == Key.Right || e.Key == Key.Space) {
			sequence.Advance ();
			ShowCurrent ();
			e.Handled = true;
			return;
		}
		if (e.Key == Key.Left) {
			sequence.Retreat ();
			ShowCurrent ();
			e.Handled = true;
			return;
		}
		base.OnKeyDown (e);
	}
}
