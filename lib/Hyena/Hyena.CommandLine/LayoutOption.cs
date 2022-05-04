//
// LayoutOption.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.CommandLine
{
	public class LayoutOption
	{
		string name;
		string description;

		public LayoutOption (string name, string description)
		{
			this.name = name;
			this.description = description;
		}

		public string Name {
			get { return name; }
		}

		public string Description {
			get { return description; }
		}
	}
}
