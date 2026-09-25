//
// TagCompletion.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Input;

/// <summary>
/// GTK <c>TagEntry.DoTagCompletion</c>: Tab completes the tag name being typed after the last
/// comma and selects the completed part; Tab again cycles forward and Shift+Tab backward.
/// Comma or Return accepts the completion; any other edit starts over.
/// </summary>
public sealed class TagCompletion
{
	readonly Func<string, IReadOnlyList<string>?> lookup;
	IReadOnlyList<string>? completions;
	int index = -1;
	string typedSoFar = string.Empty;
	int typedPosition;

	public TagCompletion (Func<string, IReadOnlyList<string>?> lookup)
		=> this.lookup = lookup;

	public readonly record struct Edit (string Text, int SelectionStart, int SelectionEnd);

	public bool IsActive => index != -1;

	public void Clear ()
	{
		index = -1;
		completions = null;
	}

	public Edit? Complete (string text, int caret, bool forward)
	{
		text ??= string.Empty;
		if (index != -1 && completions is { Count: > 0 }) {
			index = forward
				? (index + 1) % completions.Count
				: (index + completions.Count - 1) % completions.Count;
		} else {
			caret = Math.Clamp (caret, 0, text.Length);
			typedPosition = caret;
			string rightOfCursor = text.Substring (caret);
			if (rightOfCursor.Length > 1)
				return null;

			int lastComma = text.LastIndexOf (',');
			if (lastComma > caret)
				return null;

			typedSoFar = text.Substring (lastComma + 1).TrimStart (' ');
			if (typedSoFar.Length == 0)
				return null;

			completions = lookup (typedSoFar);
			if (completions is null || completions.Count == 0) {
				completions = null;
				return null;
			}

			index = forward ? 0 : completions.Count - 1;
		}

		string name = completions[index];
		string completion = name.Length >= typedSoFar.Length ? name.Substring (typedSoFar.Length) : string.Empty;
		string next = text.Substring (0, Math.Min (typedPosition, text.Length)) + completion;
		return new Edit (next, typedPosition, next.Length);
	}

	/// <summary>
	/// GTK <c>FinishTagCompletion</c>: drop the selection and add ", " after the completed name.
	/// </summary>
	public Edit? Finish (string text, int selectionEnd)
	{
		if (index == -1)
			return null;
		text ??= string.Empty;
		int position = Math.Clamp (selectionEnd, 0, text.Length);
		string next = text.Insert (position, ", ");
		Clear ();
		return new Edit (next, position + 2, position + 2);
	}
}
