/*
 * DBusService.DBusService.cs
 *
 * Author(s):
 *	Thomas Van Machelen <thomas.vanmachelen@gmail.com>
 *
 * This is free software. See COPYING for details.
 *
 */

using System;
using FSpot;
using FSpot.Extensions;
using FSpot.Utils;

namespace DBusService {
	public class DBusService : IService
	{
		public bool Start ()
		{
			uint timer = Log.InformationTimerStart ("Starting DBusService");
			try {
				DBusProxyFactory.Load (Core.Database);
			} catch {
				Log.Warning ("unable init DBus service");
			}
			Log.DebugTimerPrint (timer, "DBusService startup took {0}");
			return true;
		}

		public bool Stop ()
		{
			uint timer = Log.InformationTimerStart ("Stopping DBusService");

			DBusProxyFactory.EmitRemoteDown ();

			Log.DebugTimerPrint (timer, "DBusService stop took {0}");
			return true;
		}
	}
}
