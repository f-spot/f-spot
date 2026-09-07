//
// ProfileCatalog.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FSpot.Cms
{
	// The set of display profiles the UI offers, keyed by profile description because
	// that is what the preferences store. Two of the rules here are load-bearing and
	// neither is local to any one call site, which is why they live next to Profile
	// rather than in the directory scan that feeds them.
	public sealed class ProfileCatalog
	{
		readonly Dictionary<string, Profile> profiles = new Dictionary<string, Profile> (StringComparer.Ordinal);

		public IDictionary<string, Profile> Profiles => profiles;

		// Takes ownership: a profile the catalogue declines is closed here, because the
		// directory scan that produced it has nowhere else to put it.
		public bool Add (Profile profile)
		{
			if (profile == null)
				throw new ArgumentNullException (nameof (profile));

			// The empty description is the "colour management off" sentinel -- the
			// preference defaults to it and the dialog writes it for "None" -- so a
			// profile with no readable description must never become that key, or it
			// switches colour management on for a user who never enabled it.
			string description = profile.ProductDescription;
			if (string.IsNullOrEmpty (description)
					|| profile.ColorSpace != IccColorSpace.Rgb
					|| profiles.ContainsKey (description)) {
				profile.Dispose ();
				return false;
			}

			profiles.Add (description, profile);
			return true;
		}

		// Borrows: the caller keeps ownership, which is what ColorManagement.XProfile
		// needs since it caches and republishes the same instance.
		public bool AddShared (string key, Profile profile)
		{
			if (key == null)
				throw new ArgumentNullException (nameof (key));
			if (profile == null)
				throw new ArgumentNullException (nameof (profile));

			if (profiles.ContainsKey (key))
				return false;

			profiles.Add (key, profile);
			return true;
		}
	}
}
