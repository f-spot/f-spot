// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot.Avalonia.Services;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia;

// Avalonia counterpart of ProgressDialog (message, bar, cancel) and
// ThreadProgressDialog (retry and skip). Mail also constructs it directly
// and watches StopRequested while the attachments are written.
public partial class ProgressWindow : Window
{
	readonly CancellationTokenSource stop = new ();
	int total;
	int current;
	RetrySkipGate? retrySkipGate;

	public ProgressWindow ()
	{
		InitializeComponent ();
		Closing += (_, _) => stop.Cancel ();
	}

	public ProgressWindow (string title, int total)
	{
		InitializeComponent ();
		Configure (title, total, Strings.ProgressStop);
	}

	public static ProgressWindow Create (string title, int total, string? cancelCaption)
	{
		var window = new ProgressWindow ();
		window.Configure (title, total, cancelCaption);
		return window;
	}

	public CancellationToken StopRequested => stop.Token;

	public bool Update (string message)
	{
		current++;
		MessageBlock.Text = message;
		if (total > 0) {
			Bar.Value = Math.Min (current, total);
			CountBlock.Text = string.Format (Strings.XOfY, current, total);
			CountBlock.IsVisible = true;
		}
		return stop.IsCancellationRequested;
	}

	public Task<bool> PerformRetrySkipAsync ()
	{
		retrySkipGate = new RetrySkipGate ();
		RetryButton.Content = Strings.Retry;
		SkipButton.Content = Strings.Skip;
		AutomationProperties.SetName (RetryButton, Strings.Retry);
		AutomationProperties.SetName (SkipButton, Strings.Skip);
		RetryButton.IsVisible = true;
		SkipButton.IsVisible = true;
		return retrySkipGate.Task;
	}

	void Configure (string title, int total, string? cancelCaption)
	{
		Title = title;
		this.total = total;
		Bar.IsIndeterminate = total <= 0;
		Bar.Maximum = total > 0 ? total : 1;
		AutomationProperties.SetName (this, title);
		if (cancelCaption is null)
			return;
		CancelButton.Content = GtkMnemonic.Display (cancelCaption);
		CancelButton.IsVisible = true;
		AutomationProperties.SetName (CancelButton, cancelCaption);
	}

	void OnCancel (object? sender, RoutedEventArgs e)
		=> stop.Cancel ();

	void OnRetry (object? sender, RoutedEventArgs e)
		=> Resolve (retry: true);

	void OnSkip (object? sender, RoutedEventArgs e)
		=> Resolve (retry: false);

	void Resolve (bool retry)
	{
		RetryButton.IsVisible = false;
		SkipButton.IsVisible = false;
		retrySkipGate?.Resolve (retry);
		retrySkipGate = null;
	}
}
