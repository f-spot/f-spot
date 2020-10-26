//
// PreferenceJsonBackend.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2019 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Newtonsoft.Json.Linq;

using FSpot.Settings;
using System.Collections.Generic;
using System.Collections;

namespace FSpot.Platform
{
	class PreferenceJsonBackend
	{
		internal const string SettingsRoot = "FSpotSettings";
		internal static string PreferenceLocationOverride;

		static readonly object sync_handler = new object ();

		static JObject client;

		internal string SettingsFile { get; }

		public PreferenceJsonBackend ()
		{
			if (string.IsNullOrWhiteSpace (PreferenceLocationOverride))
				SettingsFile = Path.Combine (FSpotConfiguration.BaseDirectory, FSpotConfiguration.SettingsName);
			else
				SettingsFile = PreferenceLocationOverride;
		}

		JObject Client {
			get {
				lock (sync_handler) {
					return client ?? LoadSettings ();
				}
			}
		}

		JObject LoadSettings ()
		{
			if (!File.Exists (SettingsFile) || new FileInfo (SettingsFile).Length == 0) {
				var empty = new JObject {
					[SettingsRoot] = new JObject ()
				};
				File.WriteAllText (SettingsFile, empty.ToString ());
			}

			var settingsFile = File.ReadAllText (SettingsFile);
			var o = JObject.Parse (settingsFile);
			client = (JObject)o[SettingsRoot];
			return client;
		}

		internal void SaveSettings ()
		{
			var settings = Client.Root.ToString ();
			File.WriteAllText (SettingsFile, settings);
		}

		internal T Get<T> (string key)
		{
			if (Client.ContainsKey (key))
				return Client[key].ToObject<T> ();

			throw new NoSuchKeyException (nameof (key));
		}

		internal void Set<T> (string key, T value)
		{
			JToken token;
			if (value is IEnumerable && !(value is string))
				token = new JArray (value);
			else
				token = new JValue (value);

			if (Client[key] != null)
				Client[key].Replace (token);
			else
				Client.Add (key, token);

			// This isn't ideal, but guarantees settings will be saved for now
			SaveSettings ();
		}
	}
}
