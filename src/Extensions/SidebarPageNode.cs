/*
 * FSpot.Extensions.ServiceNode.cs
 *
 * Author(s):
 *	Ruben Vermeersch <ruben@savanne.be>
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * Copyright (c) 2010 Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using System;
using Mono.Addins;

namespace FSpot.Extensions
{
	public class SidebarPageNode : ExtensionNode {
		[NodeAttribute (Required=true)]
		protected string sidebar_page_type;

		public SidebarPage GetPage () {
			return (SidebarPage) Addin.CreateInstance (sidebar_page_type);
		}
	}
}
