//
// Utility functions.
//
// Miguel de Icaza (miguel@ximian.com).
//
// (C) 2002 Ximian, Inc.
//
//

using System.Runtime.InteropServices;
using System.Threading;
using System.Collections;
using System.IO;
using System.Text;
using System;

public class UriList : ArrayList {
	public UriList (FSpot.IBrowsableItem [] photos) {
		foreach (FSpot.IBrowsableItem p in photos) {
			Uri uri;
			try {
				uri = p.DefaultVersionUri;
			} catch {
				continue;
			}
			Add (uri);
		}
	}
	
	public UriList () : base ()
	{
	}
	
	private void LoadFromString (string data) {
		//string [] items = System.Text.RegularExpressions.Regex.Split ("\n", data);
		string [] items = data.Split ('\n');
		
		foreach (String i in items) {
			if (!i.StartsWith ("#")) {
				Uri uri;
				String s = i;

				if (i.EndsWith ("\r")) {
					s = i.Substring (0, i.Length - 1);
					Console.WriteLine ("uri = {0}", s);
				}
				
				try {
					uri = new Uri (s);
				} catch {
#if true //Workaround to bgo 362016 in gnome-screenshot. Remove this hack when gnome 2.6.18 is widely distributed.
					if (System.Text.RegularExpressions.Regex.IsMatch (s, "^file:/[^/]")) {
						try {
							s = "file:///" + s.Substring(6);
							uri = new Uri (s);
							Console.WriteLine ("Converted uri from file:/ to >>{0}<<", s);
						} catch {
							continue;
						}
					} else
						continue;
#else					
					continue;
#endif
				}
				Add (uri);
			}
		}
	}

	// NOTE: this was copied from mono's System.Uri where it is protected.
	public static string EscapeString (string str, bool escapeReserved, bool escapeHex, bool escapeBrackets) 
	{
		if (str == null)
			return String.Empty;
		
			byte [] data = Encoding.UTF8.GetBytes (str);
			StringBuilder s = new StringBuilder ();
			int len = data.Length;	
			for (int i = 0; i < len; i++) {
				char c = (char) data [i];
				// reserved    = ";" | "/" | "?" | ":" | "@" | "&" | "=" | "+" | "$" | ","
				// mark        = "-" | "_" | "." | "!" | "~" | "*" | "'" | "(" | ")"
				// control     = <US-ASCII coded characters 00-1F and 7F hexadecimal>
				// space       = <US-ASCII coded character 20 hexadecimal>
				// delims      = "<" | ">" | "#" | "%" | <">
				// unwise      = "{" | "}" | "|" | "\" | "^" | "[" | "]" | "`"
				
				// check for escape code already placed in str, 
				// i.e. for encoding that follows the pattern 
				// "%hexhex" in a string, where "hex" is a digit from 0-9 
				// or a letter from A-F (case-insensitive).
				if('%' == c && Uri.IsHexEncoding(str,i))
				{
					// if ,yes , copy it as is
					s.Append(c);
					s.Append(str[++i]);
					s.Append(str[++i]);
					continue;
				}
				
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
			
			return s.ToString ();
	}

	static char[] CharsToQuote = { ';', '?', ':', '@', '&', '=', '$', ',', '#' };

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

		return new Uri (builder.ToString (), true);
	}

	public UriList (string [] uris)
	{	
		// FIXME this is so lame do real chacking at some point
		foreach (string str in uris) {
			AddUnknown (str);
		}
	}

	public void AddUnknown (string unknown)
	{
		Uri uri;
		
		if (File.Exists (unknown) || Directory.Exists (unknown))
			uri = PathToFileUri (unknown);
		else 
			uri = new Uri (unknown);
		
		Add (uri);
	}

	public UriList (string data) 
	{
		LoadFromString (data);
	}
	
	public UriList (Gtk.SelectionData selection) 
	{
		// FIXME this should check the atom etc.
		LoadFromString (System.Text.Encoding.UTF8.GetString (selection.Data));
	}

	public new Uri [] ToArray ()
	{
		return ToArray (typeof (Uri)) as Uri [];
	}
	
	public void Add (string path)
	{
		AddUnknown (path);
	}

	public void Add (Uri uri)
	{
		Add ((object)uri);
	}

	public void Add (FSpot.IBrowsableItem item)
	{
		Add (item.DefaultVersionUri);
	}

	public override string ToString () {
		StringBuilder list = new StringBuilder ();

		foreach (Uri uri in this) {
			if (uri == null)
				break;

			list.Append (uri.ToString () + Environment.NewLine);
		}

		return list.ToString ();
	}

	public string [] ToLocalPaths () {
		int count = 0;
		foreach (Uri uri in this) {
			if (uri.IsFile)
				count++;
		}
		
		String [] paths = new String [count];
		count = 0;
		foreach (Uri uri in this) {
			if (uri.IsFile)
				paths[count++] = uri.LocalPath;
		}
		return paths;
	}
}


public class SizeUtil {
	public static string ToHumanReadable (long size)
	{
		string tmp_str = String.Empty;
		float tmp_size = size;
		int k = 0;
		string[] size_abr = {"bytes", "kB", "MB", "GB", "TB" };
		
		while (tmp_size > 700) { //it's easier to read 0.9MB than 932kB
			tmp_size = tmp_size / 1024;
				k++;
		}
		
		if (tmp_size < 7)
			tmp_str = tmp_size.ToString ("0.##");
		else if (tmp_size < 70)
			tmp_str = tmp_size.ToString ("##.#");
		else
			tmp_str = tmp_size.ToString ("#,###");
		
		if (k < size_abr.Length)
			return tmp_str + " " + size_abr[k];
		else
				return size.ToString();
	}
}


