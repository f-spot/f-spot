//
// CommandMenuItemNode.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
// Copyright (C) 2007-2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Mono.Addins;


namespace FSpot.Extensions
{
	[ExtensionNode ("Command")]
	public class CommandMenuItemNode : MenuItemNode
	{
		[NodeAttribute ("command_type", true)]
		protected string command_type;

		protected override void OnActivated (object o, EventArgs e)
		{
			var cmd = (ICommand)Addin.CreateInstance (command_type);
			cmd.Run (o, e);
		}
	}
}
