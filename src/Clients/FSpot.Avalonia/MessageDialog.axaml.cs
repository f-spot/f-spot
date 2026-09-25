using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;

using FSpot.Resources.Lang;

namespace FSpot.Avalonia;

public enum MessageDialogKind
{
	Info,
	Warning,
	Error,
	Question
}

public partial class MessageDialog : Window
{
	public MessageDialog ()
		=> InitializeComponent ();

	public static Task ShowAsync (Window owner, string heading, string body, MessageDialogKind kind = MessageDialogKind.Warning)
		=> ShowCoreAsync (owner, heading, body, Strings.Ok, showCancel: false, kind, input: null);

	public static void ShowWithoutOwner (string heading, string body, MessageDialogKind kind = MessageDialogKind.Error)
	{
		var dialog = new MessageDialog ();
		dialog.Apply (heading, body, Strings.Ok, showCancel: false, kind, input: null);
		dialog.OkButton.Click += (_, _) => dialog.Close ();
		dialog.Show ();
	}

	public static async Task<bool> ConfirmAsync (Window owner, string heading, string body, string okCaption, MessageDialogKind kind = MessageDialogKind.Warning)
	{
		string? result = await ShowCoreAsync (owner, heading, body, okCaption, showCancel: true, kind, input: null);
		return result is not null;
	}

	public static Task<string?> PromptAsync (Window owner, string heading, string prompt, string defaultValue)
		=> ShowCoreAsync (owner, heading, prompt, Strings.Ok, showCancel: true, MessageDialogKind.Question, defaultValue);

	static async Task<string?> ShowCoreAsync (Window owner, string heading, string body, string okCaption, bool showCancel, MessageDialogKind kind, string? input)
	{
		var dialog = new MessageDialog ();
		dialog.Apply (heading, body, okCaption, showCancel, kind, input);
		string? result = null;
		dialog.OkButton.Click += (_, _) => {
			result = dialog.InputBox.IsVisible ? dialog.InputBox.Text ?? string.Empty : string.Empty;
			dialog.Close ();
		};
		dialog.CancelButton.Click += (_, _) => dialog.Close ();
		await dialog.ShowDialog (owner);
		return result;
	}

	void Apply (string heading, string body, string okCaption, bool showCancel, MessageDialogKind kind, string? input)
	{
		HeadingBlock.Text = heading;
		BodyBlock.Text = body;
		BodyBlock.IsVisible = !string.IsNullOrWhiteSpace (body);
		OkButton.Content = okCaption;
		AutomationProperties.SetName (OkButton, okCaption);
		AutomationProperties.SetName (this, heading);
		CancelButton.IsVisible = showCancel;
		if (input is not null) {
			InputBox.IsVisible = true;
			InputBox.Text = input;
			AutomationProperties.SetName (InputBox, body);
		}

		(Color background, string glyph) = kind switch {
			MessageDialogKind.Error => (Color.FromRgb (0xC4, 0x2B, 0x1C), "!"),
			MessageDialogKind.Question => (Color.FromRgb (0x00, 0x78, 0xD4), "?"),
			MessageDialogKind.Info => (Color.FromRgb (0x00, 0x78, 0xD4), "i"),
			_ => (Color.FromRgb (0xC4, 0x2B, 0x1C), "!")
		};
		if (kind == MessageDialogKind.Warning)
			background = Color.FromRgb (0xC4, 0x5B, 0x12);
		IconBadge.Background = new SolidColorBrush (background);
		IconGlyph.Text = glyph;
		IconGlyph.Foreground = Brushes.White;
	}
}
