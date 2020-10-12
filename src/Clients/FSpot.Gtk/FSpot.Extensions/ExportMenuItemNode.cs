//
// ExportMenuItemNode.cs
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
			var exporter = (IExporter) Addin.CreateInstance (class_name);
			exporter.Run (SelectedImages ());
		}
	}
}
