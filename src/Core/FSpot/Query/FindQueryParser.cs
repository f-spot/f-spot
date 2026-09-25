//
// FindQueryParser.cs
//
// Ported from the GTK FindBar (Gabriel Burt, Daniel Köb, Stephane Delcroix).
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using FSpot.Core;
using FSpot.Resources.Lang;

namespace FSpot.Query
{
	/// <summary>
	/// Parses the find bar language: words joined by <c>and</c>, <c>or</c> or <c>,</c>
	/// (a comma means and), grouped with parentheses, and negated with <c>not (...)</c>.
	/// One operator per group: mixing them without parentheses is ambiguous and fails,
	/// just like the GTK find bar.
	/// </summary>
	public static class FindQueryParser
	{
		static readonly string OpStr = $"(?'Ops' {Regex.Escape (Strings.Or)} | {Regex.Escape (Strings.And)} |, )";
		const string LiteralStr = "[^{0}{1}]+?";
		const string TermStr = "(((?'Open'{0})(?'Pre'[^{0}{1}]*?))+((?'Close-Open'{1})(?'Post'[^{0}{1}]*?))+)*?(?(Open)(?!))";
		static readonly string CombTermStr = string.Format ("(?'Term'{0}|{1})|not\\s*\\((?'NotTerm'{0})\\)|not\\s*(?'NotTerm'{1})", LiteralStr, TermStr);
		static readonly string RegexStr = string.Format ("^((?'Terms'{0}){1})*(?'Terms'{0})$", CombTermStr, OpStr);
		static readonly Regex TermRegex = new Regex (string.Format (RegexStr, "\\(", "\\)"), RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Parses <paramref name="text"/>. Returns false when the text is not a valid query
		/// (unbalanced parentheses, ambiguous operators); the caller keeps its previous query then.
		/// A blank query parses to a null expression, which matches every photo.
		/// </summary>
		public static bool TryParse (string text, Func<string, Tag> tagByName, out FindExpression expression)
		{
			expression = null;
			if (string.IsNullOrWhiteSpace (text))
				return true;
			if (!ParensValid (text))
				return false;
			return TryBuild (text, tagByName, out expression);
		}

		static bool TryBuild (string text, Func<string, Tag> tagByName, out FindExpression expression)
		{
			expression = null;
			if (string.IsNullOrEmpty (text))
				return true;

			var match = TermRegex.Match (text);
			if (!match.Success)
				return false;

			string op = string.Empty;
			foreach (Capture capture in match.Groups["Ops"].Captures) {
				if (op.Length == 0)
					op = capture.Value;
				else if (!string.Equals (op, capture.Value, StringComparison.OrdinalIgnoreCase))
					return false;
			}

			if (match.Groups["Terms"].Captures.Count == 1 && match.Groups["NotTerm"].Captures.Count != 1) {
				string literal = match.Groups["Terms"].Captures[0].Value;
				expression = new FindLiteral (literal, tagByName?.Invoke (literal));
				return true;
			}

			var terms = new List<FindExpression> ();
			if (!AddSubterms (match.Groups["Term"], false, tagByName, terms))
				return false;
			if (!AddSubterms (match.Groups["NotTerm"], true, tagByName, terms))
				return false;

			if (terms.Count == 0)
				return true;
			if (terms.Count == 1) {
				expression = terms[0];
				return true;
			}
			expression = IsOr (op) ? new FindOr (terms) : new FindAnd (terms);
			return true;
		}

		static bool IsOr (string op)
			=> string.Equals (op, $" {Strings.Or} ", StringComparison.OrdinalIgnoreCase);

		static bool AddSubterms (Group group, bool negated, Func<string, Tag> tagByName, List<FindExpression> terms)
		{
			foreach (Capture capture in group.Captures) {
				string subterm = capture.Value.Trim ();
				if (subterm.Length == 0)
					continue;
				if (subterm[0] == '(' && subterm[subterm.Length - 1] == ')')
					subterm = subterm.Substring (1, subterm.Length - 2);
				if (!TryBuild (subterm, tagByName, out var child))
					return false;
				if (child == null)
					continue;
				terms.Add (negated ? new FindNot (child) : child);
			}
			return true;
		}

		public static bool ParensValid (string text)
		{
			if (text == null)
				return true;
			for (int i = 0; i < text.Length; i++) {
				if ((text[i] == '(' || text[i] == ')') && ParenPairPosition (text, i) == -1)
					return false;
			}
			return true;
		}

		static int ParenPairPosition (string text, int pos)
		{
			char one = text[pos];
			bool open = one == '(';
			char two = open ? ')' : '(';
			int count = open ? text.Length - pos - 1 : pos;
			int sames = 0;
			for (int i = 0; i < count; i++) {
				pos += open ? 1 : -1;
				if (text[pos] == one)
					sames++;
				else if (text[pos] == two) {
					if (sames == 0)
						return pos;
					sames--;
				}
			}
			return -1;
		}
	}
}
