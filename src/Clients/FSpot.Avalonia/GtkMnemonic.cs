// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

using Avalonia.Controls;
using Avalonia.Input;

namespace FSpot.Avalonia;

public static class GtkMnemonic
{
	public static string Display (string? text)
	{
		if (string.IsNullOrEmpty (text))
			return text ?? string.Empty;
		var display = new StringBuilder (text.Length);
		for (int i = 0; i < text.Length; i++) {
			if (text[i] == '_' && i + 1 < text.Length) {
				display.Append (text[++i]);
				continue;
			}
			display.Append (text[i]);
		}
		return display.ToString ();
	}

	public static Key? Key (string? text)
	{
		if (string.IsNullOrEmpty (text))
			return null;
		for (int i = 0; i < text.Length - 1; i++) {
			if (text[i] != '_')
				continue;
			char next = text[i + 1];
			if (next == '_') {
				i++;
				continue;
			}
			if (Enum.TryParse (char.ToUpperInvariant (next).ToString (), out Key key))
				return key;
		}
		return null;
	}

	public static KeyGesture? Gesture (string? text)
	{
		var key = Key (text);
		return key is null ? null : new KeyGesture (key.Value, KeyModifiers.Alt);
	}

	public static void ApplyHotKey (Button button, string? mnemonic)
	{
		var gesture = Gesture (mnemonic);
		if (gesture is not null)
			button.HotKey = gesture;
	}
}

public static class FolderPick
{
	public static string? Chosen (string? path)
		=> string.IsNullOrWhiteSpace (path) ? null : path.Trim ();
}
