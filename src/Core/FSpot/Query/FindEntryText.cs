//
// FindEntryText.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Query
{
	/// <summary>
	/// The text-editing rules of the GTK find entry, kept free of any toolkit.
	/// </summary>
	public static class FindEntryText
	{
		/// <summary>
		/// After an insertion ending at <paramref name="insertEnd"/>, adds the closing
		/// parentheses the text is missing right after the inserted text, so typing
		/// <c>(</c> yields <c>()</c> with the caret between them.
		/// </summary>
		public static string CloseParens (string text, int insertEnd)
		{
			if (string.IsNullOrEmpty (text))
				return text ?? string.Empty;
			int open = 0, close = 0;
			foreach (char c in text) {
				if (c == '(')
					open++;
				else if (c == ')')
					close++;
			}
			if (open <= close)
				return text;
			if (insertEnd < 0)
				insertEnd = 0;
			if (insertEnd > text.Length)
				insertEnd = text.Length;
			return text.Insert (insertEnd, new string (')', open - close));
		}

		/// <summary>
		/// Tab inside the entry jumps to just after the next open parenthesis.
		/// Returns null when the caret is already at the end, so Tab moves focus as usual.
		/// </summary>
		public static int? TabIntoParens (string text, int caret)
		{
			text ??= string.Empty;
			if (caret >= text.Length)
				return null;
			int pos = caret < 0 ? 0 : caret;
			while (pos < text.Length && text[pos] != '(')
				pos++;
			pos++;
			return pos > text.Length ? text.Length : pos;
		}
	}
}
