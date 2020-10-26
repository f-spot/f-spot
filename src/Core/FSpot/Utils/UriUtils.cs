//
// UriUtils.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace FSpot.Utils
{
	public static class UriUtils
	{
		// NOTE: this was copied from mono's System.Uri where it is internal.
		public static string EscapeString (string str, bool escapeReserved, bool escapeHex, bool escapeBrackets)
		{
			if (str == null)
				return string.Empty;

			StringBuilder s = new StringBuilder ();
			int len = str.Length;
			for (int i = 0; i < len; i++) {
				// mark        = "-" | "_" | "." | "!" | "~" | "*" | "'" | "(" | ")"
				// control     = <US-ASCII coded characters 00-1F and 7F hexadecimal>
				// space       = <US-ASCII coded character 20 hexadecimal>
				// delims      = "<" | ">" | "#" | "%" | <">
				// unwise      = "{" | "}" | "|" | "\" | "^" | "[" | "]" | "`"

				// check for escape code already placed in str, 
				// i.e. for encoding that follows the pattern 
				// "%hexhex" in a string, where "hex" is a digit from 0-9 
				// or a letter from A-F (case-insensitive).
				if (Uri.IsHexEncoding (str, i)) {
					// if ,yes , copy it as is
					s.Append (str.Substring (i, 3));
					i += 2;
					continue;
				}

				byte[] data = Encoding.UTF8.GetBytes (new char[] { str[i] });
				int length = data.Length;
				for (int j = 0; j < length; j++) {
					char c = (char)data[j];
					// reserved    = ";" | "/" | "?" | ":" | "@" | "&" | "=" | "+" | "$" | ","
					if ((c <= 0x20) || (c >= 0x7f) ||
						("<>%\"{}|\\^`".IndexOf (c) != -1) ||
						(escapeHex && (c == '#')) ||
						(escapeBrackets && (c == '[' || c == ']')) ||
						(escapeReserved && (";/?:@&=+$,".IndexOf (c) != -1))) {
						s.Append (Uri.HexEscape (c));
						continue;
					}
					s.Append (c);
				}
			}

			return s.ToString ();
		}
	}
}
