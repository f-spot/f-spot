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
using System.Text.Json;
using System.Text.Json.Nodes;

using FSpot.Settings;

namespace FSpot.Platform
{
	class PreferenceJsonBackend
	{
		internal const string SettingsRoot = "FSpotSettings";
		internal static string PreferenceLocationOverride;

		static readonly object sync_handler = new object ();
		static readonly JsonSerializerOptions json = new JsonSerializerOptions { WriteIndented = true };

		static JsonObject document;
		static JsonObject client;
		static string loadedFrom;

		internal static void Unload ()
		{
			lock (sync_handler) {
				document = null;
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

		JsonObject Client {
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
				document = null;
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

		JsonObject LoadSettings ()
		{
			loadedFrom = SettingsFile;
			if (!File.Exists (loadedFrom) || new FileInfo (loadedFrom).Length == 0)
				WriteEmpty (loadedFrom);

			if (!TryReadRoot (loadedFrom, out JsonObject parsed, out JsonObject root)) {
				Quarantine (loadedFrom);
				WriteEmpty (loadedFrom);
				if (!TryReadRoot (loadedFrom, out parsed, out root))
					throw new InvalidDataException (loadedFrom);
			}

			document = parsed;
			client = root;
			return client;
		}

		static bool TryReadRoot (string path, out JsonObject parsed, out JsonObject root)
		{
			parsed = null;
			root = null;
			try {
				if (JsonNode.Parse (File.ReadAllText (path)) is not JsonObject document)
					return false;
				if (document[SettingsRoot] is not JsonObject settings)
					return false;
				parsed = document;
				root = settings;
				return true;
			} catch (JsonException) {
				return false;
			}
		}

		void WriteEmpty (string path)
		{
			var empty = new JsonObject {
				[SettingsRoot] = new JsonObject ()
			};
			EnsureSettingsDirectory (path);
			ReplaceFile (path, empty.ToJsonString (json));
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

				EnsureSettingsDirectory (loadedFrom);
				ReplaceFile (loadedFrom, document.ToJsonString (json));
			}
		}

		internal T Get<T> (string key)
		{
			lock (sync_handler) {
				if (!Client.TryGetPropertyValue (key, out JsonNode node))
					throw new NoSuchKeyException (nameof (key));
				if (node is null)
					return default;
				return node.Deserialize<T> (json);
			}
		}

		internal void Set<T> (string key, T value)
		{
			JsonNode token = value is null ? JsonNode.Parse ("null") : JsonSerializer.SerializeToNode (value, json);

			lock (sync_handler) {
				Client[key] = token;
				// This isn't ideal, but guarantees settings will be saved for now
				SaveSettings ();
			}
		}
	}
}
