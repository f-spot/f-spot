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
using FSpot;
using FSpot.Extensions;
using FSpot.Utils;

namespace BeagleService {
	public class BeagleService : IService
	{
		public bool Start ()
		{
			uint timer = Log.InformationTimerStart ("Starting BeagleService");
			try {
				Core.Database.Photos.ItemsChanged += HandleDbItemsChanged;
			} catch {
				Log.Warning ("unable to hook the BeagleNotifier. are you running --view mode?");
			}
			Log.DebugTimerPrint (timer, "BeagleService startup took {0}");
			return true;
		}

		public bool Stop ()
		{
			uint timer = Log.InformationTimerStart ("Stopping BeagleService");
			Log.DebugTimerPrint (timer, "BeagleService shutdown took {0}");
			return true;
		}

		private void HandleDbItemsChanged (object sender, DbItemEventArgs<Photo> args)
		{
#if ENABLE_BEAGLE
			Log.Debug ("Notifying beagle");
			foreach (DbItem item in args.Items) {
				if (item as Photo != null)
					try {
						BeagleNotifier.SendUpdate (item as Photo);
					} catch (Exception e) {
						Log.Debug ("BeagleNotifier.SendUpdate failed with {0}", e.Message);
					}
			}
#endif
		}
	}
}
