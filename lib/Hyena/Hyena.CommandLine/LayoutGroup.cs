//
// LayoutGroup.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Hyena.CommandLine
{
	public class LayoutGroup : IEnumerable<LayoutOption>
	{
		List<LayoutOption> options;
		string id;
		string title;

		public LayoutGroup (string id, string title, List<LayoutOption> options)
		{
			this.id = id;
			this.title = title;
			this.options = options;
		}

		public LayoutGroup (string id, string title, params LayoutOption[] options)
			: this (id, title, new List<LayoutOption> (options))
		{
		}

		public IEnumerator<LayoutOption> GetEnumerator ()
		{
			return options.GetEnumerator ();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		public void Add (LayoutOption option)
		{
			options.Add (option);
		}

		public void Add (string name, string description)
		{
			options.Add (new LayoutOption (name, description));
		}

		public void Remove (LayoutOption option)
		{
			options.Remove (option);
		}

		public void Remove (string optionName)
		{
			LayoutOption option = FindOption (optionName);
			if (option != null) {
				options.Remove (option);
			}
		}

		LayoutOption FindOption (string name)
		{
			foreach (LayoutOption option in options) {
				if (option.Name == name) {
					return option;
				}
			}

			return null;
		}

		public LayoutOption this[int index] {
			get { return options[index]; }
			set { options[index] = value; }
		}

		public int Count {
			get { return options.Count; }
		}

		public string Id {
			get { return id; }
		}

		public string Title {
			get { return title; }
		}

		public IList<LayoutOption> Options {
			get { return options; }
		}
	}
}
