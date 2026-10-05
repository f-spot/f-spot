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

using FSpot.Settings;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FSpot.Platform
{
	class PreferenceJsonBackend
	{
		internal const string SettingsRoot = "FSpotSettings";
		internal static string PreferenceLocationOverride;

		static readonly object sync_handler = new object ();

		static JObject client;
		static string loadedFrom;

		internal static void Unload ()
		{
			lock (sync_handler) {
				client = null;
				loadedFrom = null;
			}
		}

		internal string SettingsFile =>
			string.IsNullOrWhiteSpace (PreferenceLocationOverride)
				? Path.Combine (FSpotConfiguration.BaseDirectory, FSpotConfiguration.SettingsName)
				: PreferenceLocationOverride;

		public PreferenceJsonBackend ()
		{
		}

		JObject Client {
			get {
				lock (sync_handler) {
					DropIfThePathChanged ();
					return client ?? LoadSettings ();
				}
			}
		}

		// The cache belongs to the path it was read from. A later override or
		// --basedir must not flush those keys into the new file.
		void DropIfThePathChanged ()
		{
			if (client != null && loadedFrom != SettingsFile) {
				client = null;
				loadedFrom = null;
			}
		}

		void EnsureSettingsDirectory (string path)
		{
			string directory = Path.GetDirectoryName (path);
			if (!string.IsNullOrEmpty (directory))
				Directory.CreateDirectory (directory);
		}

		JObject LoadSettings ()
		{
			loadedFrom = SettingsFile;
			if (!File.Exists (loadedFrom) || new FileInfo (loadedFrom).Length == 0)
				WriteEmpty (loadedFrom);

			if (!TryReadRoot (loadedFrom, out JObject root)) {
				Quarantine (loadedFrom);
				WriteEmpty (loadedFrom);
				if (!TryReadRoot (loadedFrom, out root))
					throw new InvalidDataException (loadedFrom);
			}

			client = root;
			return client;
		}

		static bool TryReadRoot (string path, out JObject root)
		{
			root = null;
			try {
				var parsed = JObject.Parse (File.ReadAllText (path));
				root = parsed[SettingsRoot] as JObject;
				return root != null;
			} catch (JsonException) {
				return false;
			}
		}

		void WriteEmpty (string path)
		{
			var empty = new JObject {
				[SettingsRoot] = new JObject ()
			};
			EnsureSettingsDirectory (path);
			ReplaceFile (path, empty.ToString ());
		}

		static void Quarantine (string path)
		{
			string dest = path + ".bad";
			if (File.Exists (dest))
				dest = path + "." + DateTime.UtcNow.ToString ("yyyyMMddHHmmssfff") + ".bad";
			File.Move (path, dest);
		}

		static void ReplaceFile (string destination, string contents)
		{
			string directory = Path.GetDirectoryName (destination);
			string temp = Path.Combine (string.IsNullOrEmpty (directory) ? "." : directory, Path.GetFileName (destination) + ".tmp");
			File.WriteAllText (temp, contents);
			try {
				if (File.Exists (destination))
					File.Replace (temp, destination, null);
				else
					File.Move (temp, destination);
			} catch {
				if (File.Exists (temp))
					File.Delete (temp);
				throw;
			}
		}

		internal void SaveSettings ()
		{
			lock (sync_handler) {
				DropIfThePathChanged ();
				if (client == null)
					LoadSettings ();

				var settings = client.Root.ToString ();
				EnsureSettingsDirectory (loadedFrom);
				ReplaceFile (loadedFrom, settings);
			}
		}

		internal T Get<T> (string key)
		{
			lock (sync_handler) {
				if (Client.ContainsKey (key))
					return Client[key].ToObject<T> ();
			}

			throw new NoSuchKeyException (nameof (key));
		}

		internal void Set<T> (string key, T value)
		{
			// JValue cannot wrap collections.
			var token = value == null ? JValue.CreateNull () : JToken.FromObject (value);

			lock (sync_handler) {
				Client[key] = token;
				// This isn't ideal, but guarantees settings will be saved for now
				SaveSettings ();
			}
		}
	}
}
