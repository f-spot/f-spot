/*
 * ExportMenuItemNode.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using Mono.Addins;
using System;

using FSpot.Core;

namespace FSpot.Extensions
{
	public delegate PhotoList SelectedImages ();

	[ExtensionNode ("ExportMenuItem")]
	public class ExportMenuItemNode : MenuItemNode
	{

		[NodeAttribute ("class", true)]
		protected string class_name;

		public static SelectedImages SelectedImages;

		protected override void OnActivated (object o, EventArgs e)
		{
			IExporter exporter = (IExporter) Addin.CreateInstance (class_name);
			exporter.Run (SelectedImages ());
		}
	}
}
