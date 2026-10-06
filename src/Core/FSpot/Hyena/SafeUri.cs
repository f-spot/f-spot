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

			return new SafeUri (path);
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
		// query, and it decodes a literal "%20" into a space. Escape only those
		// three characters. Spaces, parentheses, and other ordinary characters
		// stay as they are, because the catalog compares the stored text.
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
					AppendEscapedSegments (builder, body.Substring (split), slash.EndsWith ("/", StringComparison.Ordinal));
				return builder.ToString ();
			}

			if (HasDrive (slash)) {
				builder.Append ('/');
				builder.Append (slash[0]);
				builder.Append (':');
				AppendEscapedSegments (builder, slash.Substring (2), slash.EndsWith ("/", StringComparison.Ordinal));
				return builder.ToString ();
			}

			AppendEscapedSegments (builder, slash, slash.EndsWith ("/", StringComparison.Ordinal));
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

		static bool HasDrive (string slash)
		{
			return slash.Length >= 3
				&& char.IsLetter (slash[0])
				&& slash[1] == ':'
				&& slash[2] == '/';
		}

		static void AppendEscapedSegments (StringBuilder builder, string path, bool trailingSlash)
		{
			int start = 0;
			bool wroteSegment = false;
			while (start < path.Length) {
				if (path[start] == '/') {
					start++;
					continue;
				}

				int relative = path.IndexOf ('/', start);
				string segment = relative < 0 ? path.Substring (start) : path.Substring (start, relative - start);
				builder.Append ('/');
				builder.Append (EscapePathSegment (segment));
				wroteSegment = true;
				if (relative < 0)
					break;
				start = relative + 1;
			}

			// "file://" already ends in a slash. A path of only slashes, such
			// as "/", still needs the root slash that makes "file:///".
			if (!wroteSegment) {
				if (trailingSlash)
					builder.Append ('/');
				return;
			}

			if (trailingSlash && builder[builder.Length - 1] != '/')
				builder.Append ('/');
		}

		// '%' first, so the escapes added for '#' and '?' are not escaped again.
		static string EscapePathSegment (string segment)
		{
			return segment.Replace ("%", "%25").Replace ("#", "%23").Replace ("?", "%3F");
		}

		public static string UriToFilename (string uri)
		{
			return new Uri (EscapeRawFileDelimiters (uri)).LocalPath;
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
