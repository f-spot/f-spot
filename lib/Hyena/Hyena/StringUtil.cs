//
// StringUtil.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hyena
{
	public static class StringUtil
	{
		static CompareOptions compare_options =
			CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace |
			CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth;

		public static int RelaxedIndexOf (string haystack, string needle)
		{
			return ApplicationContext.CurrentCulture.CompareInfo.IndexOf (haystack, needle, compare_options);
		}

		public static int RelaxedCompare (string a, string b)
		{
			if (a == null && b == null) {
				return 0;
			} else if (a != null && b == null) {
				return 1;
			} else if (a == null && b != null) {
				return -1;
			}

			int a_offset = a.StartsWith ("the ") ? 4 : 0;
			int b_offset = b.StartsWith ("the ") ? 4 : 0;

			return ApplicationContext.CurrentCulture.CompareInfo.Compare (a, a_offset, a.Length - a_offset,
				b, b_offset, b.Length - b_offset, compare_options);
		}

		public static string CamelCaseToUnderCase (string s)
		{
			return CamelCaseToUnderCase (s, '_');
		}

		static Regex camelcase = new Regex ("([A-Z]{1}[a-z]+)", RegexOptions.Compiled);
		public static string CamelCaseToUnderCase (string s, char underscore)
		{
			if (string.IsNullOrEmpty (s)) {
				return null;
			}

			var undercase = new StringBuilder ();
			string[] tokens = camelcase.Split (s);

			for (int i = 0; i < tokens.Length; i++) {
				if (tokens[i] == string.Empty) {
					continue;
				}

				undercase.Append (tokens[i].ToLower (System.Globalization.CultureInfo.InvariantCulture));
				if (i < tokens.Length - 2) {
					undercase.Append (underscore);
				}
			}

			return undercase.ToString ();
		}

		public static string UnderCaseToCamelCase (string s)
		{
			if (string.IsNullOrEmpty (s)) {
				return null;
			}

			var builder = new StringBuilder ();

			for (int i = 0, n = s.Length, b = -1; i < n; i++) {
				if (b < 0 && s[i] != '_') {
					builder.Append (char.ToUpper (s[i]));
					b = i;
				} else if (s[i] == '_' && i + 1 < n && s[i + 1] != '_') {
					if (builder.Length > 0 && char.IsUpper (builder[builder.Length - 1])) {
						builder.Append (char.ToLower (s[i + 1]));
					} else {
						builder.Append (char.ToUpper (s[i + 1]));
					}
					i++;
					b = i;
				} else if (s[i] != '_') {
					builder.Append (char.ToLower (s[i]));
					b = i;
				}
			}

			return builder.ToString ();
		}

		public static string RemoveNewlines (string input)
		{
			if (input != null) {
				return input.Replace ("\r\n", string.Empty).Replace ("\n", string.Empty);
			}
			return null;
		}

		static Regex tags = new Regex ("<[^>]+>", RegexOptions.Compiled | RegexOptions.Multiline);
		public static string RemoveHtml (string input)
		{
			if (input == null) {
				return input;
			}

			return tags.Replace (input, string.Empty);
		}

		public static string DoubleToTenthsPrecision (double num)
		{
			return DoubleToTenthsPrecision (num, false);
		}

		public static string DoubleToTenthsPrecision (double num, bool always_decimal)
		{
			return DoubleToTenthsPrecision (num, always_decimal, NumberFormatInfo.CurrentInfo);
		}

		public static string DoubleToTenthsPrecision (double num, bool always_decimal, IFormatProvider provider)
		{
			num = Math.Round (num, 1, MidpointRounding.ToEven);
			return string.Format (provider, !always_decimal && num == (int)num ? "{0:N0}" : "{0:N1}", num);
		}

		// This method helps us pluralize doubles. Probably a horrible i18n idea.
		public static int DoubleToPluralInt (double num)
		{
			if (num == (int)num)
				return (int)num;
			else
				return (int)num + 1;
		}

		// A mapping of non-Latin characters to be considered the same as
		// a Latin equivalent.
		static Dictionary<char, char> BuildSpecialCases ()
		{
			var dict = new Dictionary<char, char> ();
			dict['\u00f8'] = 'o';
			dict['\u0142'] = 'l';
			return dict;
		}
		static Dictionary<char, char> searchkey_special_cases = BuildSpecialCases ();

		//  Removes accents from Latin characters, and some kinds of punctuation.
		public static string SearchKey (string val)
		{
			if (string.IsNullOrEmpty (val)) {
				return val;
			}

			val = val.ToLower ();
			var sb = new StringBuilder ();
			UnicodeCategory category;
			bool previous_was_letter = false;
			bool got_space = false;

			// Normalizing to KD splits into (base, combining) so we can check for letters
			// and then strip off any NonSpacingMarks following them
			foreach (char orig_c in val.TrimStart ().Normalize (NormalizationForm.FormKD)) {

				// Check for a special case *before* whitespace. This way, if
				// a special case is ever added that maps to ' ' or '\t', it
				// won't cause a run of whitespace in the result.
				char c = orig_c;
				if (searchkey_special_cases.ContainsKey (c)) {
					c = searchkey_special_cases[c];
				}

				if (c == ' ' || c == '\t') {
					got_space = true;
					continue;
				}

				category = char.GetUnicodeCategory (c);
				if (category == UnicodeCategory.OtherPunctuation) {
					// Skip punctuation
				} else if (!(previous_was_letter && category == UnicodeCategory.NonSpacingMark)) {
					if (got_space) {
						sb.Append (' ');
						got_space = false;
					}
					sb.Append (c);
				}

				// Can ignore A-Z because we've already lowercased the char
				previous_was_letter = char.IsLetter (c);
			}

			string result = sb.ToString ();
			try {
				result = result.Normalize (NormalizationForm.FormKC);
			} catch {
				// FIXME: work-around, see http://bugzilla.gnome.org/show_bug.cgi?id=590478
			}
			return result;
		}

		static Regex invalid_path_regex = BuildInvalidPathRegex ();

		static Regex BuildInvalidPathRegex ()
		{
			char[] invalid_path_characters = new char[] {
                // Control characters: there's no reason to ever have one of these in a track name anyway,
                // and they're invalid in all Windows filesystems.
                '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07',
				'\x08', '\x09', '\x0A', '\x0B', '\x0C', '\x0D', '\x0E', '\x0F',
				'\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17',
				'\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D', '\x1E', '\x1F',

                // Invalid in FAT32 / NTFS: " \ / : * | ? < >
                // Invalid in HFS   :
                // Invalid in ext3  /
                '"', '\\', '/', ':', '*', '|', '?', '<', '>'
			};

			string regex_str = "[";
			for (int i = 0; i < invalid_path_characters.Length; i++) {
				regex_str += "\\" + invalid_path_characters[i];
			}
			regex_str += "]+";

			return new Regex (regex_str, RegexOptions.Compiled);
		}

		static CompareInfo culture_compare_info = ApplicationContext.CurrentCulture.CompareInfo;
		public static byte[] SortKey (string orig)
		{
			if (orig == null) { return null; }
			return culture_compare_info.GetSortKey (orig, CompareOptions.IgnoreCase).KeyData;
		}

		static readonly char[] escape_path_trim_chars = new char[] { '.', '\x20' };
		public static string EscapeFilename (string input)
		{
			if (input == null)
				return "";

			// Remove leading and trailing dots and spaces.
			input = input.Trim (escape_path_trim_chars);

			return invalid_path_regex.Replace (input, "_");
		}

		public static string EscapePath (string input)
		{
			if (input == null)
				return "";

			// This method should be called before the full path is constructed.
			if (Path.IsPathRooted (input)) {
				return input;
			}

			var builder = new StringBuilder ();
			foreach (string name in input.Split (Path.DirectorySeparatorChar)) {
				// Escape the directory or the file name.
				string escaped = EscapeFilename (name);
				// Skip empty names.
				if (escaped.Length > 0) {
					builder.Append (escaped);
					builder.Append (Path.DirectorySeparatorChar);
				}
			}

			// Chop off the last character.
			if (builder.Length > 0) {
				builder.Length--;
			}

			return builder.ToString ();
		}

		public static string MaybeFallback (string input, string fallback)
		{
			string trimmed = input?.Trim ();
			return string.IsNullOrEmpty (trimmed) ? fallback : trimmed;
		}

		public static uint SubstringCount (string haystack, string needle)
		{
			if (string.IsNullOrEmpty (haystack) || string.IsNullOrEmpty (needle)) {
				return 0;
			}

			int position = 0;
			uint count = 0;
			while (true) {
				int index = haystack.IndexOf (needle, position);
				if (index < 0) {
					return count;
				}
				count++;
				position = index + 1;
			}
		}

		public static string SubstringBetween (this string input, string start, string end)
		{
			int s = input.IndexOf (start);
			if (s == -1)
				return null;

			s += start.Length;
			int l = Math.Min (input.Length - 1, input.IndexOf (end, s)) - s;
			if (l > 0 && s + l < input.Length) {
				return input.Substring (s, l);
			} else {
				return null;
			}
		}

		static readonly char[] escaped_like_chars = new char[] { '\\', '%', '_' };
		public static string EscapeLike (string s)
		{
			if (s.IndexOfAny (escaped_like_chars) != -1) {
				return s.Replace (@"\", @"\\").Replace ("%", @"\%").Replace ("_", @"\_");
			}
			return s;
		}

		public static string Join (this IEnumerable<string> strings, string sep)
		{
			var sb = new StringBuilder ();
			foreach (var str in strings) {
				sb.Append (str);
				sb.Append (sep);
			}

			if (sb.Length > 0 && sep != null) {
				sb.Length -= sep.Length;
			}

			return sb.ToString ();
		}

		public static IEnumerable<object> FormatInterleaved (string format, params object[] objects)
		{
			var indices = new Dictionary<object, int> ();

			for (int i = 0; i < objects.Length; i++) {
				int j = format.IndexOf ("{" + i + "}");
				if (j == -1) {
					Hyena.Log.ErrorFormat ("Translated string {0} should contain {{1}} in which to place object {2}", format, i, objects[i]);
				}
				indices[objects[i]] = j;
			}

			int str_pos = 0;
			foreach (var obj in objects.OrderBy (w => indices[w])) {
				int widget_i = indices[obj];
				if (widget_i > str_pos) {
					var str = format.Substring (str_pos, widget_i - str_pos).Trim ();
					if (str != "") yield return str;
				}

				yield return obj;
				str_pos = widget_i + 2 + Array.IndexOf (objects, obj).ToString ().Length;
			}

			if (str_pos < format.Length - 1) {
				var str = format.Substring (str_pos, format.Length - str_pos).Trim ();
				if (str != "") yield return str;
			}
		}
	}
}
