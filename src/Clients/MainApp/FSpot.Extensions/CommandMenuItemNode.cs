/*
 * FSpot.Extensions.CommandMenuItemNode
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
	[ExtensionNode ("Command")]
	public class CommandMenuItemNode : MenuItemNode
	{

		[NodeAttribute ("command_type", true)]
		protected string command_type;

		protected override void OnActivated (object o, EventArgs e)
		{
			ICommand cmd = (ICommand) Addin.CreateInstance (command_type);
			cmd.Run (o, e);
		}
	}
}
