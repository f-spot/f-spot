/*
 * BeagleService.BeagleService.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using System;
using FSpot.Extensions;
using FSpot.Utils;

namespace BeagleService {
	public class BeagleService : IService
	{
		public bool Start ()
		{
			uint timer = Log.InformationTimerStart ("Starting BeagleService");
			Log.DebugTimerPrint (timer, "BeagleService startup took {0}");
			return true;
		}

		public bool Stop ()
		{
			uint timer = Log.InformationTimerStart ("Starting BeagleService");
			Log.DebugTimerPrint (timer, "BeagleService startup took {0}");	
			return true;
		}
	}
}
