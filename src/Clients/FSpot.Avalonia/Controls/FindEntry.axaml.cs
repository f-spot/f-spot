//
// FindEntry.axaml.cs
//
// The find bar entry: GTK's FindBar entry with its LogicEntryCompletion.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

using FSpot.Query;
using FSpot.Widgets;

namespace FSpot.Avalonia.Controls;

public partial class FindEntry : UserControl
{
	public static readonly StyledProperty<string?> TextProperty =
		AvaloniaProperty.Register<FindEntry, string?> (nameof (Text), defaultBindingMode: BindingMode.TwoWay);

	public static readonly StyledProperty<IEnumerable<string>?> TagNamesProperty =
		AvaloniaProperty.Register<FindEntry, IEnumerable<string>?> (nameof (TagNames));

	public static readonly StyledProperty<ICommand?> EscapeCommandProperty =
		AvaloniaProperty.Register<FindEntry, ICommand?> (nameof (EscapeCommand));

	CompletionLogic? completion;
	string lastText = string.Empty;
	bool editing;

	public FindEntry ()
	{
		InitializeComponent ();
		Box.TextChanged += OnBoxTextChanged;
		Box.AddHandler (KeyDownEvent, OnBoxKeyDown, RoutingStrategies.Tunnel);
		Completions.AddHandler (TappedEvent, (_, _) => AcceptCompletion ());
	}

	public string? Text {
		get => GetValue (TextProperty);
		set => SetValue (TextProperty, value);
	}

	public IEnumerable<string>? TagNames {
		get => GetValue (TagNamesProperty);
		set => SetValue (TagNamesProperty, value);
	}

	/// <summary>Runs on Escape; the find bar clears and hides itself, as in GTK.</summary>
	public ICommand? EscapeCommand {
		get => GetValue (EscapeCommandProperty);
		set => SetValue (EscapeCommandProperty, value);
	}

	public TextBox Entry => Box;

	public bool IsCompletionOpen => CompletionPopup.IsOpen;

	public IReadOnlyList<string> CompletionItems => Completions.ItemsSource as IReadOnlyList<string> ?? [];

	public void FocusEntry ()
		=> Box.Focus ();

	protected override void OnGotFocus (FocusChangedEventArgs e)
	{
		base.OnGotFocus (e);
		if (ReferenceEquals (e.Source, this))
			Box.Focus ();
	}

	protected override void OnPropertyChanged (AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged (change);
		if (change.Property == TextProperty && !editing) {
			string text = change.GetNewValue<string?> () ?? string.Empty;
			if (Box.Text != text) {
				editing = true;
				lastText = text;
				Box.Text = text;
				editing = false;
				CloseCompletions ();
			}
		}
	}

	void OnBoxTextChanged (object? sender, TextChangedEventArgs e)
	{
		string text = Box.Text ?? string.Empty;
		if (editing || text == lastText)
			return;
		int caret = Box.CaretIndex;
		bool inserted = text.Length > lastText.Length;
		if (inserted) {
			string closed = FindEntryText.CloseParens (text, caret);
			if (closed != text) {
				editing = true;
				Box.Text = closed;
				Box.CaretIndex = caret;
				editing = false;
				text = closed;
			}
		}
		lastText = text;
		editing = true;
		Text = text;
		editing = false;
		if (inserted)
			UpdateCompletions (text, caret);
		else
			CloseCompletions ();
	}

	void UpdateCompletions (string text, int caret)
	{
		completion = new CompletionLogic ();
		var matches = FindCompletion.Matches (TagNames, text, caret, completion);
		if (matches.Count == 0) {
			CloseCompletions ();
			return;
		}
		Completions.ItemsSource = matches;
		Completions.SelectedIndex = -1;
		CompletionPopup.IsOpen = true;
	}

	void CloseCompletions ()
	{
		CompletionPopup.IsOpen = false;
		Completions.ItemsSource = null;
	}

	void AcceptCompletion ()
	{
		if (completion is null || Completions.SelectedItem is not string name)
			return;
		string text = Box.Text ?? string.Empty;
		int pos = Box.CaretIndex;
		string replaced = completion.ReplaceKey (text, name, ref pos);
		CloseCompletions ();
		editing = true;
		lastText = replaced;
		Box.Text = replaced;
		Box.CaretIndex = Math.Clamp (pos, 0, replaced.Length);
		Text = replaced;
		editing = false;
		Box.Focus ();
	}

	void OnBoxKeyDown (object? sender, KeyEventArgs e)
	{
		bool open = CompletionPopup.IsOpen;
		switch (e.Key) {
		case Key.Escape:
			if (open)
				CloseCompletions ();
			else if (EscapeCommand?.CanExecute (null) == true)
				EscapeCommand.Execute (null);
			e.Handled = true;
			break;
		case Key.Down when open:
			Completions.SelectedIndex = Math.Min (Completions.SelectedIndex + 1, CompletionItems.Count - 1);
			e.Handled = true;
			break;
		case Key.Up when open:
			Completions.SelectedIndex = Math.Max (Completions.SelectedIndex - 1, 0);
			e.Handled = true;
			break;
		case Key.Enter when open && Completions.SelectedItem is not null:
			AcceptCompletion ();
			e.Handled = true;
			break;
		case Key.Tab when e.KeyModifiers == KeyModifiers.None:
			if (open && Completions.SelectedItem is not null) {
				AcceptCompletion ();
				e.Handled = true;
				break;
			}
			if (FindEntryText.TabIntoParens (Box.Text, Box.CaretIndex) is int next) {
				CloseCompletions ();
				Box.CaretIndex = next;
				e.Handled = true;
			}
			break;
		}
	}
}

/// <summary>The tag names GTK's LogicEntryCompletion offers for the word under the caret.</summary>
public static class FindCompletion
{
	public static IReadOnlyList<string> Matches (IEnumerable<string>? names, string? text, int caret, CompletionLogic logic)
	{
		if (names is null || string.IsNullOrEmpty (text))
			return [];
		string key = text.Normalize (NormalizationForm.FormC);
		var matches = new List<string> ();
		foreach (string name in names) {
			if (!matches.Contains (name) && logic.MatchFunc (name, key, caret - 1))
				matches.Add (name);
		}
		return matches;
	}
}
