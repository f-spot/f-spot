//
// Layout.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Hyena.CommandLine
{
	public class Layout
	{
		List<LayoutGroup> groups;

		public Layout (List<LayoutGroup> groups)
		{
			this.groups = groups;
		}

		public Layout (params LayoutGroup[] groups) : this (new List<LayoutGroup> (groups))
		{
		}

		int TerminalWidth {
			get { return Console.WindowWidth <= 0 ? 80 : Console.WindowWidth; }
		}

		public string ToString (params string[] groupIds)
		{
			return ToString (GroupIdsToGroups (groupIds));
		}

		public override string ToString ()
		{
			return ToString (groups);
		}

		public string ToString (IEnumerable<LayoutGroup> groups)
		{
			var builder = new StringBuilder ();

			int min_spacing = 6;

			int group_index = 0;
			int group_count = 0;
			int max_option_length = 0;
			int max_description_length = 0;
			int description_alignment = 0;

			foreach (LayoutGroup group in groups) {
				foreach (LayoutOption option in group) {
					if (option.Name.Length > max_option_length) {
						max_option_length = option.Name.Length;
					}
				}
			}

			max_description_length = TerminalWidth - max_option_length - min_spacing - 4;
			description_alignment = max_option_length + min_spacing + 4;

			IEnumerator<LayoutGroup> enumerator = groups.GetEnumerator ();
			while (enumerator.MoveNext ()) {
				group_count++;
			}

			foreach (LayoutGroup group in groups) {
				if (group.Id != "default") {
					builder.Append (group.Title);
					builder.AppendLine ();
					builder.AppendLine ();
				}

				for (int i = 0, n = group.Count; i < n; i++) {
					int spacing = (max_option_length - group[i].Name.Length) + min_spacing;
					builder.AppendFormat ("  --{0}{2}{1}", group[i].Name,
						WrapAlign (group[i].Description, max_description_length,
							description_alignment, i == n - 1),
						string.Empty.PadRight (spacing));
					builder.AppendLine ();
				}

				if (group_index++ < group_count - 1) {
					builder.AppendLine ();
				}
			}

			return builder.ToString ();
		}

		public string LayoutLine (string str)
		{
			return WrapAlign (str, TerminalWidth, 0, true);
		}

		static string WrapAlign (string str, int width, int align, bool last)
		{
			var builder = new StringBuilder ();
			bool did_wrap = false;

			for (int i = 0, b = 0; i < str.Length; i++, b++) {
				if (str[i] == ' ') {
					int word_length = 0;
					for (int j = i + 1; j < str.Length && str[j] != ' '; word_length++, j++) ;

					if (b + word_length >= width) {
						builder.AppendLine ();
						builder.Append (string.Empty.PadRight (align));
						b = 0;
						did_wrap = true;
						continue;
					}
				}

				builder.Append (str[i]);
			}

			if (did_wrap && !last) {
				builder.AppendLine ();
			}

			return builder.ToString ();
		}

		public void Add (LayoutGroup group)
		{
			groups.Add (group);
		}

		public void Remove (LayoutGroup group)
		{
			groups.Remove (group);
		}

		public void Remove (string groupId)
		{
			LayoutGroup group = FindGroup (groupId);
			if (group != null) {
				groups.Remove (group);
			}
		}

		LayoutGroup FindGroup (string id)
		{
			foreach (LayoutGroup group in groups) {
				if (group.Id == id) {
					return group;
				}
			}

			return null;
		}

		IEnumerable<LayoutGroup> GroupIdsToGroups (string[] groupIds)
		{
			foreach (string group_id in groupIds) {
				LayoutGroup group = FindGroup (group_id);
				if (group != null) {
					yield return group;
				}
			}
		}

		public static LayoutOption Option (string name, string description)
		{
			return new LayoutOption (name, description);
		}

		public static LayoutGroup Group (string id, string title, params LayoutOption[] options)
		{
			return new LayoutGroup (id, title, options);
		}
	}
}
