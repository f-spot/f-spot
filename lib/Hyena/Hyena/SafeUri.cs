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
using System.Runtime.InteropServices;

namespace Hyena
{
	public class SafeUri
	{
		private enum LocalPathCheck
		{
			NotPerformed,
			Yes,
			No
		}

		private static int MAX_SCHEME_LENGTH = 8;

		private string uri;
		private string local_path;
		private string scheme;
		private LocalPathCheck local_path_check = LocalPathCheck.NotPerformed;

		public SafeUri (string uri)
		{
			if (string.IsNullOrEmpty (uri))
				throw new ArgumentNullException (nameof (uri));

			int scheme_delimit_index = uri.IndexOf ("://", StringComparison.InvariantCulture);
			if (scheme_delimit_index > 0 && scheme_delimit_index <= MAX_SCHEME_LENGTH) {
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
				this.uri = uriOrFilename;
			} else {
				this.uri = FilenameToUri (uriOrFilename);
			}
		}

		public SafeUri (Uri uri)
		{
			if (uri == null)
				throw new ArgumentNullException (nameof (uri));

			this.uri = uri.AbsoluteUri;
		}

		public static string FilenameToUri (string localPath)
		{
			return new Uri (new Uri ("file://"), localPath).ToString ();
		}

		public static string UriToFilename (string uri)
		{
			return new Uri (uri).LocalPath;
		}
		public static string UriToFilename (SafeUri uri)
		{
			return UriToFilename (uri.AbsoluteUri);
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
			SafeUri s = o as SafeUri;
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

		public bool IsLocalPath {
			get {
				if (local_path_check == LocalPathCheck.NotPerformed) {
					if (IsFile) {
						local_path_check = LocalPathCheck.Yes;
						return true;
					} else {
						local_path_check = LocalPathCheck.No;
						return false;
					}
				}

				return local_path_check == LocalPathCheck.Yes;
			}
		}

		public string AbsolutePath {
			get {
				if (local_path == null && IsLocalPath)
					local_path = UriToFilename (uri);

				return local_path;
			}
		}

		public string LocalPath {
			get { return AbsolutePath; }
		}

		public string Scheme {
			get {
				if (scheme == null) {
					scheme = uri.Substring (0, uri.IndexOf ("://", StringComparison.InvariantCulture));
				}

				return scheme;
			}
		}

		public bool IsFile => Scheme == Uri.UriSchemeFile;
	}
}
