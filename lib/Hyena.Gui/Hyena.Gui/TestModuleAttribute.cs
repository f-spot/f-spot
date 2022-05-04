//
// TestModuleAttribute.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Gui
{
	class TestModuleAttribute : Attribute
	{
		string name;
		public string Name {
			get { return name; }
			set { name = value; }
		}

		public TestModuleAttribute (string name)
		{
			this.name = name;
		}
	}
}
