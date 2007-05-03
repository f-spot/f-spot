/*
 * ToolMenuItemNode.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using Mono.Addins;
using System;

namespace FSpot.Extensions
{
	[ExtensionNode ("ToolMenuItem")]
	public class ToolMenuItemNode : MenuItemNode
	{
		[NodeAttribute ("command_type", true)]
		string command_type;

		protected override void OnActivated (object o, EventArgs e)
		{
			ITool tool = (ITool) Addin.CreateInstance (command_type);
			tool.Run ();
		}
	}
}
