//
// Preferences.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Daniel Köb <daniel.koeb@peony.at>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <stephane@delcroix.org>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2019 Stephen Shaw
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2005-2010 Novell, Inc.
// Copyright (C) 2007, 2010 Ruben Vermeersch
// Copyright (C) 2006-2009 Stephane Delcroix
// Copyright (C) 2005-2006 Larry Ewing
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;

using FSpot.Platform;

using Hyena;

using Mono.Unix;

namespace FSpot.Settings
{
	public static partial class Preferences
	{
		static readonly object sync_handler = new object ();
		static readonly Dictionary<string, object> cache = new Dictionary<string, object> ();

		static readonly Dictionary<string, object> defaults = new Dictionary<string, object> {
			{ MainWindowX, 0 },
			{ MainWindowY, 0 },
			{ MainWindowHeight, 0 },
			{ MainWindowWidth, 0 },
			{ ImportWindowHeight, 0 },
			{ ImportWindowWidth, 0 },
			{ ImportWindowPanePosition, 0 },
			{ FilmstripOrientation, 0 },
			{ MetadataEmbedInImage, false },
			{ MetadataAlwaysUseSidecar, false },
			{ MainWindowMaximized, false },
			{ GroupAdaptorOrderAsc, false },
			{ ImportRemoveOriginals, false },
			{ GlassPosition, null },
			{ ShowToolbar, true },
			{ ShowSidebar, true },
			{ ShowTimeline, true },
			{ ShowFilmstrip, true },
			{ ShowTags, true },
			{ ShowDates, true },
			{ ShowRatings, true },
			{ ViewerShawFilenames, true },
			{ TagIconSize, (int)IconSize.Medium },
			{ TagIconAutomatic, true },
			{ SidebarPosition, 130 },
			{ Zoom, null },
			{ ImportGuiRollHistory, 10 },
			{ ScreensaverTag, 1 },
			{ ScreensaverDelay, 4.0 },
			{ StoragePath, Path.Combine(FSpotConfiguration.HomeDirectory, Catalog.GetString ("Photos")) },
			{ ExportEmailSize, 3 }, // medium size 640px
			{ ExportEmailRotate, true },
			{ ViewerInterpolation, true },
			{ ViewerTransparency, "NONE" },
			{ ViewerTransColor, "#000000" },
			{ EditRedeyeThreshold, -15 },
			{ GtkRc, string.Empty },
			{ ColorManagementDisplayProfile, string.Empty },
			{ ColorManagementDisplayOutputProfile, string.Empty },
			{ ImportCheckDuplicates, true },
			{ ImportCopyFiles, true },
			{ ImportIncludeSubfolders, true },
			{ ImportMergeRawAndJpeg, true },
			{ MailToCommand, string.Empty },
			{ MailToEnabled, false },
			{ ThumbsMaxAge, -1 },
			{ ThumbsMaxSize, -1 }
		};

		static PreferenceBackend backend;
		static EventHandler<NotifyEventArgs> changed_handler;
		internal static PreferenceBackend Backend {
			get {
				if (backend == null) {
					backend = new PreferenceBackend ();
					changed_handler = OnSettingChanged;
					// FIXME, Bring this back?
					//backend.AddNotify (APP_FSPOT, changed_handler);
					//backend.AddNotify (GNOME_MAILTO, changed_handler);
				}
				return backend;
			}
		}

		public static T Get<T> (string key)
		{
			var _ = TryGet (key, out T result);
			return result;
		}

		public static bool TryGet<T> (string key, out T result)
		{
			result = default;

			// Check cache
			if (cache.TryGetValue (key, out object cachedValue)) {
				result = (T)cachedValue;
				return true;
			}

			// Check preference backend, set default in backend
			try {
				result = Backend.Get<T> (key);
			} catch (NoSuchKeyException ex) {
				if (defaults.TryGetValue (key, out object defaultValue)) {
					Backend.Set (key, defaultValue);
					result = (T)defaultValue;
				} else {
					Log.Exception ($"[Preferences] No key found: {key}", ex);
				}
			}

			// Update cache
			cache.Add (key, result);

			try {
				return true;
			} catch (InvalidCastException ex) {
				Log.Exception ($"[Preferences] Invalid cast: {key}", ex);
				return false;
			}
		}

		public static void Set (string key, object value)
		{
			lock (sync_handler) {
				try {
					cache[key] = value;
					Backend.Set (key, value);
				} catch (Exception ex) {
					Log.Exception ($"[Preferences] Unable to set this : {key}", ex);
				}
			}
		}

		public static event EventHandler<NotifyEventArgs> SettingChanged;

		static void OnSettingChanged (object sender, NotifyEventArgs args)
		{
			lock (sync_handler) {
				if (cache.ContainsKey (args.Key)) {
					cache[args.Key] = args.Value;
				}
			}

			SettingChanged?.Invoke (sender, args);
		}
	}
}
