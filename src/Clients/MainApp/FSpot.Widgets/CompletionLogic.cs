//
//  CompletionLogic.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007 Gabriel Burt
// Copyright (C) 2010 Daniel Köb
// Copyright (C) 2007-2008 Stephane Delcroix
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

using Mono.Unix;

using Hyena;

namespace FSpot.Widgets
{
	public class CompletionLogic
	{
		string last_key = string.Empty;
		string transformed_key = string.Empty;
		int start = 0;

		static string or_op = " " + Catalog.GetString ("or") + " ";
		static string and_op = " " + Catalog.GetString ("and") + " ";

		static int or_op_len = or_op.Length;
		static int and_op_len = and_op.Length;

		public bool MatchFunc (string name, string key, int pos)
		{
			// If this is the fist comparison for this key, convert the key (which is the entire search string)
			// into just the part that is relevant to completing this tag name.
			if (key != last_key) {
				last_key = key;

				if (key == null || key.Length == 0 || pos < 0 || pos > key.Length - 1)
					transformed_key = string.Empty;
				else if (key [pos] == '(' || key [pos] == ')' || key [pos] == ',')
					transformed_key = string.Empty;
				else {
					start = 0;
					for (int i = pos; i >= 0; i--) {
						if (key [i] == ')' || key [i] == '(' ||
						   (i >= and_op_len - 1 && string.Compare (key.Substring (i - and_op_len + 1, and_op_len), and_op, true) == 0) ||
						   (i >= or_op_len - 1 && string.Compare (key.Substring (i - or_op_len + 1, or_op_len), or_op, true) == 0)) {
							//Log.DebugFormat ("have start break char at {0}", i);
							start = i + 1;
							break;
						}
					}

					int end = key.Length - 1;
					for (int j = pos; j < key.Length; j++) {
						if (key [j] == ')' || key [j] == '(' ||
						   (key.Length >= j + and_op_len && string.Compare (key.Substring (j, and_op_len), and_op, true) == 0) ||
						   (key.Length >= j + or_op_len && string.Compare (key.Substring (j, or_op_len), or_op, true) == 0)) {
							end = j - 1;
							break;
						}
					}

					//Log.DebugFormat ("start = {0} end = {1}", start, end);

					int len = end - start + 1;
					if (len > 0 && start < last_key.Length)
						transformed_key = last_key.Substring (start, end - start + 1);
					else
						transformed_key = string.Empty;
				}
				//Log.DebugFormat ("transformed key {0} into {1}", key, transformed_key);
			}

			if (transformed_key == string.Empty)
				return false;

			// Ignore null or names that are too short
			if (name == null || name.Length <= transformed_key.Length)
				return false;

			//Log.DebugFormat ("entered = {0} compared to {1}", transformed_key, name);

			// Try to match key and name case insensitive
			if (string.Compare (transformed_key, name.Substring (0, transformed_key.Length), true) == 0) {
				return true;
			}

			// Try to match with diacritics removed from name
			string simplified_name = StringUtil.SearchKey (name.Substring (0, transformed_key.Length));
			//Log.DebugFormat ("entered = {0} compared to {1}", transformed_key, simplified_name);
			return (string.Compare (transformed_key, simplified_name, true) == 0);
		}

		public string ReplaceKey (string query, string name, ref int pos)
		{
			// do some sanity checks first
			if (start > query.Length) {
				Log.Error ("ReplaceKey: start > query.length");
				return query;
			}
			// move caret after inserted name, even if it was not
			// at the end of the key
			pos = start + name.Length;
			return query.Substring (0, start) + name + query.Substring (start + transformed_key.Length);
		}
	}
}
