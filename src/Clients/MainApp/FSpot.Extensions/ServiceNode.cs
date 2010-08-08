/*
 * FSpot.Extensions.ServiceNode.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using System;
using Mono.Addins;

namespace FSpot.Extensions
{
	public class ServiceNode : ExtensionNode
	{
		[NodeAttribute ("class", true)]
		protected string class_name;

		IService service = null;

		public void Initialize ()
		{
			service = Addin.CreateInstance (class_name) as IService;
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
