//
// UriUtils.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
				if (Uri.IsHexEncoding (str,i)) {
					// if ,yes , copy it as is
					s.Append(str.Substring (i, 3));
					i += 2;
					continue;
				}

				byte [] data = Encoding.UTF8.GetBytes (new char[] {str[i]});
				int length = data.Length;
				for (int j = 0; j < length; j++) {
					char c = (char) data [j];
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
