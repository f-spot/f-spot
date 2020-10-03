//
// ServiceNode.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Mono.Addins;

namespace FSpot.Extensions
{
	public class ServiceNode : ExtensionNode
	{
		[NodeAttribute ("class", true)]
		protected string ClassName { get; set; }

		IService service;

		public void Initialize ()
		{
			service = Addin.CreateInstance (ClassName) as IService;
		}

		public bool Start ()
		{
			if (service == null)
				throw new Exception ("Service not initialized. Call Initialize () prior to Start() or Stop()");
			return service.Start ();
		}

		public bool Stop ()
		{
			if (service == null)
				throw new Exception ("Service not initialized. Call Initialize () prior to Start() or Stop()");
			return service.Stop ();
		}
	}
}
