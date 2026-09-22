// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Platform;
using FSpot.Settings;

namespace FSpot
{
	public sealed class AddinStatus
	{
		public AddinStatus (string id, string name, bool enabled)
		{
			Id = id ?? throw new ArgumentNullException (nameof (id));
			Name = name ?? id;
			Enabled = enabled;
		}

		public string Id { get; }
		public string Name { get; }
		public bool Enabled { get; set; }
	}

	public static class AddinCatalog
	{
		public static bool IsEnabled (string id)
		{
			if (string.IsNullOrEmpty (id))
				return false;
			try {
				return Preferences.JsonBackend.Get<bool> (Preferences.ExtensionKey + id);
			} catch (NoSuchKeyException) {
				return true;
			}
		}

		public static void SetEnabled (IList<AddinStatus> addins, string id, bool enabled)
		{
			if (addins == null)
				throw new ArgumentNullException (nameof (addins));
			foreach (var addin in addins) {
				if (addin.Id != id)
					continue;
				addin.Enabled = enabled;
				Preferences.Set (Preferences.ExtensionKey + id, enabled);
				return;
			}
		}
	}
}
