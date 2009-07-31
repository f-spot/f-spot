/*
 * FSpot.Utils.UriUtils.cs
 *
 * Author(s):
 *	Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details
 */

using System;
using System.Text;
using System.IO;

namespace FSpot.Utils
{
	public static class UriUtils
	{		
		public static string UriToStringEscaped (Uri uri)
		{
			return EscapeString (uri.ToString (), false, true, false);
		}
	
		public static string PathToFileUriEscaped (string path)
		{
			return UriToStringEscaped (PathToFileUri (path));
		}
	
		public static Uri PathToFileUri (string path)
		{
			path = Path.GetFullPath (path);
	
			StringBuilder builder = new StringBuilder ();
			builder.Append (Uri.UriSchemeFile);
			builder.Append (Uri.SchemeDelimiter);
	
			int i;
			while ((i = path.IndexOfAny (CharsToQuote)) != -1) {
				if (i > 0)
					builder.Append (path.Substring (0, i));
				builder.Append (Uri.HexEscape (path [i]));
				path = path.Substring (i+1);
			}
			builder.Append (path);
	
			return new Uri (builder.ToString ());
		}

		static char[] CharsToQuote = { ';', '?', ':', '@', '&', '=', '$', ',', '#', '%' };
		// NOTE: this was copied from mono's System.Uri where it is internal.
		public static string EscapeString (string str, bool escapeReserved, bool escapeHex, bool escapeBrackets) 
		{
			if (str == null)
				return String.Empty;
			
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
