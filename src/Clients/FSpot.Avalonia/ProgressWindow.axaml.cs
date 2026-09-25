// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot.Resources.Lang;

namespace FSpot.Avalonia;

// FSpot.UI.Dialog.ProgressDialog with a Stop button: counts through a fixed number
// of steps and reports when the user asked to stop.
public partial class ProgressWindow : Window
{
	readonly CancellationTokenSource stop = new ();

	public ProgressWindow ()
		: this (string.Empty, 1)
	{
	}

	public ProgressWindow (string title, int total)
	{
		InitializeComponent ();
		Title = title;
		Bar.Maximum = Math.Max (1, total);
		StopButton.Content = GtkMnemonic.Display (Strings.ProgressStop);
		Closing += (_, _) => stop.Cancel ();
	}

	public CancellationToken StopRequested => stop.Token;

	// ProgressDialog.Update: shows message, advances one step, returns whether to stop.
	public bool Update (string message)
	{
		Message.Text = message;
		Bar.Value = Math.Min (Bar.Maximum, Bar.Value + 1);
		return stop.IsCancellationRequested;
	}

	void OnStop (object? sender, RoutedEventArgs e)
		=> stop.Cancel ();
}
