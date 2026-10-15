//
// TagCompletionInput.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia.Input;

/// <summary>
/// GTK <c>TagEntry</c> completion on a tag text box: Tab and Shift+Tab cycle through the tags
/// that start with the name being typed. Tab leaves the box when nothing matches.
/// Comma or Return accepts the completion.
/// </summary>
public static class TagCompletionInput
{
	public static readonly AttachedProperty<bool> EnableProperty =
		AvaloniaProperty.RegisterAttached<TextBox, bool> ("Enable", typeof (TagCompletionInput));

	static readonly AttachedProperty<TagCompletion?> CompletionProperty =
		AvaloniaProperty.RegisterAttached<TextBox, TagCompletion?> ("Completion", typeof (TagCompletionInput));

	static readonly AttachedProperty<bool> ApplyingProperty =
		AvaloniaProperty.RegisterAttached<TextBox, bool> ("Applying", typeof (TagCompletionInput));

	static TagCompletionInput ()
		=> EnableProperty.Changed.AddClassHandler<TextBox> ((box, args) => {
			if (!args.GetNewValue<bool> () || box.GetValue (CompletionProperty) is not null)
				return;
			box.SetValue (CompletionProperty, new TagCompletion (prefix => WindowCommand.Model (box)?.TagNamesStartingWith (prefix)));
			box.AddHandler (InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
			box.PropertyChanged += OnPropertyChanged;
		});

	public static void SetEnable (TextBox element, bool value) => element.SetValue (EnableProperty, value);
	public static bool GetEnable (TextBox element) => element.GetValue (EnableProperty);

	static void OnPropertyChanged (object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		// GTK TagEntry.OnChanged: any edit other than the completion itself starts over.
		if (e.Property != TextBox.TextProperty || sender is not TextBox box || box.GetValue (ApplyingProperty))
			return;
		box.GetValue (CompletionProperty)?.Clear ();
	}

	static void OnKeyDown (object? sender, KeyEventArgs e)
	{
		if (sender is not TextBox box || box.GetValue (CompletionProperty) is not TagCompletion completion)
			return;
		string text = box.Text ?? string.Empty;
		switch (e.Key) {
		case Key.Tab:
			if ((e.KeyModifiers & ~KeyModifiers.Shift) != KeyModifiers.None)
				return;
			if (completion.Complete (text, box.CaretIndex, (e.KeyModifiers & KeyModifiers.Shift) == 0) is not TagCompletion.Edit edit)
				break;
			Apply (box, edit);
			e.Handled = true;
			break;
		case Key.OemComma:
			if (!completion.IsActive)
				return;
			if (completion.Finish (text, SelectionEnd (box)) is TagCompletion.Edit comma)
				Apply (box, comma);
			e.Handled = true;
			break;
		case Key.Enter:
			if (completion.IsActive && completion.Finish (text, SelectionEnd (box)) is TagCompletion.Edit enter)
				Apply (box, enter);
			break;
		}
	}

	static int SelectionEnd (TextBox box)
		=> Math.Max (box.SelectionStart, box.SelectionEnd);

	static void Apply (TextBox box, TagCompletion.Edit edit)
	{
		box.SetValue (ApplyingProperty, true);
		try {
			box.Text = edit.Text;
			box.CaretIndex = edit.SelectionEnd;
			box.SelectionStart = edit.SelectionStart;
			box.SelectionEnd = edit.SelectionEnd;
		} finally {
			box.SetValue (ApplyingProperty, false);
		}
	}
}
