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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;
using FSpot.Utils;

namespace FSpot.Widgets
{
	public class CompletionLogic
	{
		string last_key = string.Empty;
		string transformed_key = string.Empty;
		int start = 0;

		static string or_op = $" {Strings.Or} ";
		static string and_op = $" {Strings.And} ";

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
				else if (key[pos] == '(' || key[pos] == ')' || key[pos] == ',')
					transformed_key = string.Empty;
				else {
					start = 0;
					for (int i = pos; i >= 0; i--) {
						if (key[i] == ')' || key[i] == '(' ||
						   (i >= and_op_len - 1 && string.Compare (key.Substring (i - and_op_len + 1, and_op_len), and_op, true) == 0) ||
						   (i >= or_op_len - 1 && string.Compare (key.Substring (i - or_op_len + 1, or_op_len), or_op, true) == 0)) {
							//Logger.Log.Debug ($"have start break char at {i}");
							start = i + 1;
							break;
						}
					}

					int end = key.Length - 1;
					for (int j = pos; j < key.Length; j++) {
						if (key[j] == ')' || key[j] == '(' ||
						   (key.Length >= j + and_op_len && string.Compare (key.Substring (j, and_op_len), and_op, true) == 0) ||
						   (key.Length >= j + or_op_len && string.Compare (key.Substring (j, or_op_len), or_op, true) == 0)) {
							end = j - 1;
							break;
						}
					}

					//Logger.Log.Debug ($"start = {start} end = {end}");

					int len = end - start + 1;
					if (len > 0 && start < last_key.Length)
						transformed_key = last_key.Substring (start, end - start + 1);
					else
						transformed_key = string.Empty;
				}
				//Logger.Log.Debug ($"transformed key {key} into {transformed_key}");
			}

			if (transformed_key == string.Empty)
				return false;

			// Ignore null or names that are too short
			if (name == null || name.Length <= transformed_key.Length)
				return false;

			//Logger.Log.Debug ($"entered = {transformed_key} compared to {name}");

			// Try to match key and name case insensitive
			if (string.Compare (transformed_key, name.Substring (0, transformed_key.Length), true) == 0) {
				return true;
			}

			// Try to match with diacritics removed from namee
			string simplified_name = StringsUtils.Simplify (name.Substring (0, transformed_key.Length));
			//Logger.Log.Debug ($"entered =eo {transformed_key} compared to {simplified_name}");
			return (string.Compare (transformed_key, simplified_name, true) == 0);
		}

		public string ReplaceKey (string query, string name, ref int pos)
		{
			// do some sanity checks first
			if (start > query.Length) {
				Logger.Log.Error ("ReplaceKey: start > query.length");
				return query;
			}
			// move caret after inserted name, even if it was not
			// at the end of the key
			pos = start + name.Length;
			return query.Substring (0, start) + name + query.Substring (start + transformed_key.Length);
		}
	}
}
