//
// SidebarPageNode.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Mono.Addins;

namespace FSpot.Extensions
{
	public class SidebarPageNode : ExtensionNode
	{
		[NodeAttribute (Required=true)]
		protected string sidebarPageType;

		public SidebarPage GetPage ()
		{
			return (SidebarPage) Addin.CreateInstance (sidebarPageType);
		}
	}
}
