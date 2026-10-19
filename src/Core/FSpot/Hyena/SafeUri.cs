/***************************************************************************
 *  SafeUri.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW:
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),
 *  to deal in the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hyena
{
	public class SafeUri
	{
		const int MaxSchemeLength = 8;

		readonly string uri;
		string local_path;
		string scheme;

		public SafeUri (string uri)
		{
			if (string.IsNullOrEmpty (uri))
				throw new ArgumentNullException (nameof (uri));

			uri = NormalizeSingleSlashFileUri (uri);
			int scheme_delimit_index = uri.IndexOf ("://", StringComparison.Ordinal);
			if (scheme_delimit_index > 0 && scheme_delimit_index <= MaxSchemeLength) {
				this.uri = uri;
			} else {
				this.uri = FilenameToUri (uri);
			}
		}

		public SafeUri (string uriOrFilename, bool isUri)
		{
			if (string.IsNullOrEmpty (uriOrFilename)) {
				throw new ArgumentNullException (nameof (uriOrFilename));
			}

			if (isUri) {
				uri = NormalizeSingleSlashFileUri (uriOrFilename);
			} else {
				uri = FilenameToUri (uriOrFilename);
			}
		}

		public static SafeUri FromPath (string path)
		{
			if (string.IsNullOrEmpty (path))
				throw new ArgumentNullException (nameof (path));

			return new SafeUri (path, false);
		}

		// file:/path has no "://", but it is still a file URI. Some drag sources
		// emit that form. file:///path is the spelling the rest of this type stores.
		static string NormalizeSingleSlashFileUri (string text)
		{
			if (text.Length > 5
				&& text.StartsWith ("file:", StringComparison.OrdinalIgnoreCase)
				&& text[5] == '/'
				&& (text.Length == 6 || text[6] != '/'))
				return "file://" + text.Substring (5);

			return text;
		}

		// A path is not a URI. System.Uri treats '#' as a fragment and '?' as a
		// query, and it decodes a literal "%20" into a space. Escape those, plus
		// the non-characters and private-use code points System.Uri percent-encodes,
		// so a folder written by an older catalog still compares equal. Spaces and
		// parentheses stay as they are. '.' and '..' resolve the same way.
		static string FilenameToUri (string localPath)
		{
			string slash = WithUriSlashes (localPath);
			var builder = new StringBuilder ("file://");
			if (slash.StartsWith ("//", StringComparison.Ordinal) && !slash.StartsWith ("///", StringComparison.Ordinal)) {
				string body = slash.TrimStart ('/');
				int split = body.IndexOf ('/');
				string host = split < 0 ? body : body.Substring (0, split);
				builder.Append (Uri.EscapeDataString (host));
				if (split >= 0)
					AppendFileSegments (builder, body.Substring (split), slash.EndsWith ("/", StringComparison.Ordinal), true, true, EscapePathSegment);
				return builder.ToString ();
			}

			AppendFileSegments (builder, slash, slash.EndsWith ("/", StringComparison.Ordinal), true, true, EscapePathSegment);
			return builder.ToString ();
		}

		// On Windows '\' is the separator. On POSIX it is a legal file-name
		// character, except when the string is a Windows drive or UNC path
		// passed through on another platform.
		static string WithUriSlashes (string localPath)
		{
			if (Path.DirectorySeparatorChar == '\\' || IsWindowsDriveOrUnc (localPath))
				return localPath.Replace ('\\', '/');

			return localPath;
		}

		static bool IsWindowsDriveOrUnc (string path)
		{
			if (path.StartsWith ("\\\\", StringComparison.Ordinal))
				return true;

			return path.Length >= 3
				&& char.IsLetter (path[0])
				&& path[1] == ':'
				&& (path[2] == '\\' || path[2] == '/');
		}

		// FileUri calls this with a strict escape. SafeUri keeps an interior
		// empty segment and a root slash, and encodes a trailing space only on
		// the last segment. A final "." or ".." is the directory itself, so the
		// slash stays. A drive letter is special only when the path is not
		// already rooted, so "/c:/x" is a file name.
		internal static void AppendFileSegments (StringBuilder builder, string path, bool trailingSlash, bool keepEmptySegments, bool ensureRootSlash, Func<string, bool, string> escape)
		{
			string[] parts = path.Split ('/');
			bool rooted = path.StartsWith ("/", StringComparison.Ordinal);
			int begin = rooted ? 1 : 0;
			int end = parts.Length;
			if (trailingSlash && end > begin && parts[end - 1].Length == 0)
				end--;

			var segments = new List<string> ();
			bool collapsedDot = false;
			bool seenSegment = false;
			for (int i = begin; i < end; i++) {
				string segment = parts[i];
				if (segment == "." || segment == "..") {
					collapsedDot = true;
					if (segment == ".." && segments.Count > 0 && (rooted || !IsDriveRoot (segments)))
						segments.RemoveAt (segments.Count - 1);
					continue;
				}

				collapsedDot = false;
				if (segment.Length == 0 && (!keepEmptySegments || !seenSegment))
					continue;

				if (segment.Length > 0)
					seenSegment = true;

				segments.Add (segment);
			}

			if (collapsedDot)
				trailingSlash = true;

			// "file://" already ends in a slash. A path of only slashes, or a
			// path that resolves to the root, still needs the slash in "file:///".
			bool anyContent = false;
			for (int i = 0; i < segments.Count; i++) {
				if (segments[i].Length > 0) {
					anyContent = true;
					break;
				}
			}

			if (!anyContent) {
				if (ensureRootSlash)
					builder.Append ('/');
				return;
			}

			int first = 0;
			if (!rooted && IsDrivePrefix (segments[0])) {
				builder.Append ('/');
				builder.Append (segments[0]);
				first = 1;
			}

			for (int i = first; i < segments.Count; i++) {
				bool last = i == segments.Count - 1 && !trailingSlash;
				builder.Append ('/');
				builder.Append (escape (segments[i], last));
			}

			if (trailingSlash)
				builder.Append ('/');
		}

		static bool IsDrivePrefix (string segment)
		{
			return segment.Length == 2
				&& char.IsLetter (segment[0])
				&& segment[1] == ':';
		}

		static bool IsDriveRoot (List<string> segments)
		{
			return segments.Count == 1 && IsDrivePrefix (segments[0]);
		}

		// A new file name, not a segment already stored in the catalog.
		internal static string EscapeFileName (string filename)
		{
			return EscapePathSegment (filename, true);
		}

		static string EscapePathSegment (string segment, bool escapeTrailingWhitespace)
		{
			int trail = segment.Length;
			if (escapeTrailingWhitespace) {
				while (trail > 0 && IsPathTrailingWhitespace (segment[trail - 1]))
					trail--;
			}

			var builder = new StringBuilder (segment.Length);
			int index = 0;
			while (index < segment.Length) {
				int codePoint;
				int width;
				if (char.IsHighSurrogate (segment[index])
					&& index + 1 < segment.Length
					&& char.IsLowSurrogate (segment[index + 1])) {
					codePoint = char.ConvertToUtf32 (segment[index], segment[index + 1]);
					width = 2;
				} else {
					codePoint = segment[index];
					width = 1;
				}

				if (index >= trail || MustPercentEncode (codePoint))
					AppendPercentUtf8 (builder, codePoint);
				else
					builder.Append (segment, index, width);

				index += width;
			}

			return builder.ToString ();
		}

		static bool IsPathTrailingWhitespace (char value)
		{
			return value == ' ' || value == '\t' || value == '\n' || value == '\r';
		}

		// The same code points System.Uri percent-encodes in a file path, plus
		// '#', '?', and '%'. Other characters, including spaces, stay literal.
		static bool MustPercentEncode (int codePoint)
		{
			if (codePoint == '%' || codePoint == '#' || codePoint == '?')
				return true;
			if (codePoint >= 0x80 && codePoint <= 0x9F)
				return true;
			if (codePoint >= 0xE000 && codePoint <= 0xF8FF)
				return true;
			if (codePoint >= 0xFDD0 && codePoint <= 0xFDEF)
				return true;
			if (codePoint >= 0xFFF0 && codePoint <= 0xFFFF)
				return true;
			if (codePoint > 0xFFFF && (codePoint & 0xFFFE) == 0xFFFE)
				return true;
			if (codePoint >= 0xE0000 && codePoint <= 0xE0FFF)
				return true;
			if (codePoint >= 0xF0000)
				return true;
			return false;
		}

		static void AppendPercentUtf8 (StringBuilder builder, int codePoint)
		{
			byte[] bytes = Encoding.UTF8.GetBytes (char.ConvertFromUtf32 (codePoint));
			for (int i = 0; i < bytes.Length; i++) {
				builder.Append ('%');
				builder.Append (bytes[i].ToString ("X2"));
			}
		}

		public static string UriToFilename (string uri)
		{
			string path = new Uri (EscapeRawFileDelimiters (uri)).LocalPath;
			// LocalPath rewrites a Windows drive or UNC to '\' on every host.
			// A POSIX name that contains a backslash is not that shape and stays.
			if (Path.DirectorySeparatorChar != '\\' && IsWindowsDriveOrUnc (path))
				return path.Replace ('\\', '/');
			return path;
		}

		// A file URI stored with a raw '#' or '?' still names one path. Those
		// characters are not a fragment or a query in this catalog.
		static string EscapeRawFileDelimiters (string uri)
		{
			if (string.IsNullOrEmpty (uri) || !uri.StartsWith ("file://", StringComparison.OrdinalIgnoreCase))
				return uri;

			return uri.Replace ("#", "%23").Replace ("?", "%3F");
		}

		public override string ToString ()
		{
			return AbsoluteUri;
		}

		public static implicit operator string (SafeUri s)
		{
			return s.ToString ();
		}

		public override bool Equals (object o)
		{
			var s = o as SafeUri;
			if (s != null) {
				return s.AbsoluteUri == AbsoluteUri;
			}

			return false;
		}

		public override int GetHashCode ()
		{
			return AbsoluteUri.GetHashCode ();
		}

		public string AbsoluteUri {
			get { return uri; }
		}

		public string AbsolutePath {
			get {
				if (!IsFile)
					return null;

				return local_path ??= UriToFilename (uri);
			}
		}

		public string LocalPath {
			get { return AbsolutePath; }
		}

		public string Scheme {
			get {
				if (scheme == null) {
					int split = uri.IndexOf ("://", StringComparison.Ordinal);
					scheme = split > 0 ? uri.Substring (0, split) : string.Empty;
				}

				return scheme;
			}
		}

		bool IsFile => Scheme == Uri.UriSchemeFile;
	}
}
