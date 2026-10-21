//
// PreferencesTests.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2019 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

using FSpot.Platform;
using FSpot.Settings;
using FSpot.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FSpot.Preferences.Tests
{
	[TestClass]
	public class PreferencesTests
	{
		string TestSettingsFile;
		string previousOverride;
		PreferenceJsonBackend jsonBackend;

		JsonObject LoadSettings (string location)
		{
			var parsed = JsonNode.Parse (File.ReadAllText (location)) as JsonObject;
			return parsed[PreferenceJsonBackend.SettingsRoot] as JsonObject;
		}

		[TestInitialize]
		public void Setup ()
		{
			previousOverride = PreferenceJsonBackend.PreferenceLocationOverride;
			var tmpFile = Path.GetTempFileName ();
			var jsonfile = Path.ChangeExtension (tmpFile, "json");
			File.Move (tmpFile, jsonfile);
			PreferenceJsonBackend.PreferenceLocationOverride = TestSettingsFile = jsonfile;
			PreferenceJsonBackend.Unload ();
			jsonBackend = new PreferenceJsonBackend ();
		}

		[TestCleanup]
		public void TearDown ()
		{
			if (File.Exists (TestSettingsFile))
				File.Delete (TestSettingsFile);
			PreferenceJsonBackend.PreferenceLocationOverride = previousOverride;
			PreferenceJsonBackend.Unload ();
		}

		void AssertSettingsFileWritten ()
			=> Assert.IsGreaterThan (0, new FileInfo (TestSettingsFile).Length);

		[TestMethod]
		public void CanSetStringSetting ()
		{
			const string key = Settings.Preferences.StoragePath;
			const string v = "StoragePathString";
			jsonBackend.Set (key, v);
			jsonBackend.SaveSettings ();
			AssertSettingsFileWritten ();
			Assert.AreEqual (v, LoadSettings (TestSettingsFile)[key].Deserialize<string> ());
		}

		[TestMethod]
		public void CanSetBoolSetting ()
		{
			const string key = Settings.Preferences.ExportKey;
			jsonBackend.Set (key, true);
			jsonBackend.SaveSettings ();
			AssertSettingsFileWritten ();
			Assert.IsTrue (LoadSettings (TestSettingsFile)[key].Deserialize<bool> ());
		}

		[TestMethod]
		public void CanSetDoubleSetting ()
		{
			const string key = Settings.Preferences.CustomCropRatios;
			jsonBackend.Set (key, 0.0);
			jsonBackend.SaveSettings ();
			AssertSettingsFileWritten ();
			Assert.AreEqual (0.0, LoadSettings (TestSettingsFile)[key].Deserialize<double> ());
		}

		[TestMethod]
		public void CanSetSettingWithCollection ()
		{
			var key = Settings.Preferences.ExpandedTags;
			var list = new List<int> ();

			jsonBackend.Set (key, list);
			jsonBackend.SaveSettings ();
			AssertSettingsFileWritten ();

			var settings = LoadSettings (TestSettingsFile);
			var result = settings[key].Deserialize<List<int>> ();

			Assert.AreSequenceEqual (list, result);
		}

		[TestMethod]
		public void CanGetStringSettings ()
		{
			const string key = Settings.Preferences.StoragePath;
			const string v = "StoragePathString";
			jsonBackend.Set (key, v);
			jsonBackend.SaveSettings ();
			AssertSettingsFileWritten ();
			Assert.AreEqual (v, jsonBackend.Get<string> (key));
		}

		[TestMethod]
		public void CanGetBoolSettings ()
		{
			const string key = Settings.Preferences.ExportKey;
			jsonBackend.Set (key, true);
			jsonBackend.SaveSettings ();
			AssertSettingsFileWritten ();
			Assert.IsTrue (jsonBackend.Get<bool> (key));
		}

		[TestMethod]
		public void CanGetDoubleSettings ()
		{
			const string key = Settings.Preferences.CustomCropRatios;
			jsonBackend.Set (key, 0.0);
			jsonBackend.SaveSettings ();
			AssertSettingsFileWritten ();
			Assert.AreEqual (0.0, jsonBackend.Get<double> (key));
		}

		[TestMethod]
		public void UnsetSettingThrowsNoSuchKeyException ()
		{
			Assert.ThrowsExactly<NoSuchKeyException> (() => jsonBackend.Get<bool> ("RandomKey123"));
		}

		[TestMethod]
		public void LoadSettings_CreatesMissingParentDirectory ()
		{
			var dirs = new TemporaryDirectories ();
			try {
				PreferenceJsonBackend.Unload ();
				string settingsFile = Path.Combine (dirs.Create ("prefs"), "missing", "f-spot-settings.json");
				PreferenceJsonBackend.PreferenceLocationOverride = settingsFile;

				Assert.ThrowsExactly<NoSuchKeyException> (() => new PreferenceJsonBackend ().Get<bool> ("RandomKey123"));
				Assert.IsTrue (File.Exists (settingsFile));
			} finally {
				PreferenceJsonBackend.Unload ();
				dirs.DeleteAll ();
			}
		}

		// Preferences returns the default value instead of an exception
		[TestMethod]
		public void PreferencesGetDefaultSetting ()
		{
			var result = Settings.Preferences.Get<bool> (Settings.Preferences.TagIconAutomatic);
			Assert.IsTrue (result);
		}

		[TestMethod]
		public void ChangingTheSettingsPath_DoesNotFlushTheOldCache ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-prefs-" + Guid.NewGuid ().ToString ("n"));
			string fileA = Path.Combine (root, "a.json");
			string unoverridden = Path.Combine (root, FSpotConfiguration.SettingsName);
			string previousOverride = PreferenceJsonBackend.PreferenceLocationOverride;
			string previousBase = FSpotConfiguration.BaseDirectory;
			string key = "Extension/FSpot.Test.NeverSaved." + Guid.NewGuid ().ToString ("n");
			try {
				Directory.CreateDirectory (root);
				PreferenceJsonBackend.PreferenceLocationOverride = fileA;
				PreferenceJsonBackend.Unload ();
				jsonBackend.Set (key, true);

				FSpotConfiguration.BaseDirectory = root;
				PreferenceJsonBackend.PreferenceLocationOverride = null;
				jsonBackend.Set (Settings.Preferences.ShowSidebar, false);

				string flushed = File.ReadAllText (unoverridden);
				Assert.DoesNotContain (key, flushed);
				Assert.Contains (key, File.ReadAllText (fileA));
			} finally {
				PreferenceJsonBackend.PreferenceLocationOverride = previousOverride;
				FSpotConfiguration.BaseDirectory = previousBase;
				PreferenceJsonBackend.Unload ();
				if (Directory.Exists (root))
					Directory.Delete (root, true);
			}
		}

		[TestMethod]
		public void TruncatedSettings_AreMovedAsideAndStartEmpty ()
		{
			const string broken = "{ \"FSpotSettings\": {";
			File.WriteAllText (TestSettingsFile, broken);
			PreferenceJsonBackend.Unload ();

			var backend = new PreferenceJsonBackend ();

			Assert.ThrowsExactly<NoSuchKeyException> (() => backend.Get<bool> ("Missing"));
			Assert.IsNotNull (LoadSettings (TestSettingsFile));
			string[] quarantined = Directory.GetFiles (
				Path.GetDirectoryName (TestSettingsFile),
				Path.GetFileName (TestSettingsFile) + "*.bad");
			Assert.HasCount (1, quarantined);
			Assert.AreEqual (broken, File.ReadAllText (quarantined[0]));
		}

		[TestMethod]
		public void SettingsWithoutARoot_AreMovedAsideAndStartEmpty ()
		{
			File.WriteAllText (TestSettingsFile, "{}");
			PreferenceJsonBackend.Unload ();

			var backend = new PreferenceJsonBackend ();

			Assert.ThrowsExactly<NoSuchKeyException> (() => backend.Get<bool> ("Missing"));
			Assert.IsNotNull (LoadSettings (TestSettingsFile));
		}

		[TestMethod]
		public void SaveSettings_LeavesNoTempFile ()
		{
			jsonBackend.Set ("RandomKey", "kept");
			jsonBackend.SaveSettings ();

			Assert.IsFalse (File.Exists (TestSettingsFile + ".tmp"));
			Assert.AreEqual ("kept", LoadSettings (TestSettingsFile)["RandomKey"].Deserialize<string> ());
		}

		[TestMethod]
		public void ResetIncorrectTypeInSettings ()
		{
			Settings.Preferences.Set ("RandomKey", "WrongValue");
			var result = Settings.Preferences.TryGet ("RandomKey", out double randomValue);

			Assert.IsTrue (result);
			Assert.AreEqual (0.0, randomValue);
		}

		[TestMethod]
		public void Peek_DoesNotCreateAMissingSettingsFileOrInsertADefault ()
		{
			var dirs = new TemporaryDirectories ();
			string previous = PreferenceJsonBackend.PreferenceLocationOverride;
			string key = "Extension/DevelopInUfraw/JpegQuality";
			try {
				PreferenceJsonBackend.Unload ();
				string settingsFile = Path.Combine (dirs.Create ("prefs"), "absent", "f-spot-settings.json");
				PreferenceJsonBackend.PreferenceLocationOverride = settingsFile;

				bool missingFile = Settings.Preferences.Peek (key, out int unread);
				Assert.IsFalse (missingFile);
				Assert.AreEqual (0, unread);
				Assert.IsFalse (File.Exists (settingsFile));

				Directory.CreateDirectory (Path.GetDirectoryName (settingsFile));
				File.WriteAllText (settingsFile, "{ \"FSpotSettings\": { \"Other\": 1 } }");
				PreferenceJsonBackend.Unload ();

				bool missingKey = Settings.Preferences.Peek (key, out int stillUnset);
				Assert.IsFalse (missingKey);
				Assert.AreEqual (0, stillUnset);
				Assert.DoesNotContain (key, File.ReadAllText (settingsFile));

				bool found = Settings.Preferences.Peek ("Other", out int stored);
				Assert.IsTrue (found);
				Assert.AreEqual (1, stored);
			} finally {
				PreferenceJsonBackend.PreferenceLocationOverride = previous;
				PreferenceJsonBackend.Unload ();
				dirs.DeleteAll ();
			}
		}

		[TestMethod]
		public void Set_RaisesSettingChangedOnlyWhenTheValueChanges ()
		{
			var keys = new List<string> ();
			var values = new List<object> ();
			EventHandler<NotifyEventArgs> handler = (_, args) => {
				keys.Add (args.Key);
				values.Add (args.Value);
			};
			Settings.Preferences.SettingChanged += handler;
			try {
				Settings.Preferences.Set (Settings.Preferences.ColorManagementDisplayProfile, "studio.icc");
				Settings.Preferences.Set (Settings.Preferences.ColorManagementDisplayProfile, "studio.icc");
				Settings.Preferences.Set (Settings.Preferences.ColorManagementDisplayProfile, "other.icc");
				Settings.Preferences.Set (Settings.Preferences.StoragePath, "/photos/library");

				Assert.AreSequenceEqual (new[] {
					Settings.Preferences.ColorManagementDisplayProfile,
					Settings.Preferences.ColorManagementDisplayProfile,
					Settings.Preferences.StoragePath
				}, keys);
				Assert.AreEqual ("studio.icc", values[0]);
				Assert.AreEqual ("other.icc", values[1]);
				Assert.AreEqual ("/photos/library", values[2]);
			} finally {
				Settings.Preferences.SettingChanged -= handler;
			}
		}

		[TestMethod]
		public void Get_ReadsAFileWrittenAsNestedObjects ()
		{
			File.WriteAllText (TestSettingsFile, $$"""
				{
				  "FSpotSettings": {
				    "{{Settings.Preferences.StoragePath}}": "from-disk",
				    "{{Settings.Preferences.Zoom}}": 1.5,
				    "{{Settings.Preferences.ExpandedTags}}": [3, 4]
				  }
				}
				""");
			PreferenceJsonBackend.Unload ();

			var backend = new PreferenceJsonBackend ();

			Assert.AreEqual ("from-disk", backend.Get<string> (Settings.Preferences.StoragePath));
			Assert.AreEqual (1.5, backend.Get<double> (Settings.Preferences.Zoom));
			Assert.AreSequenceEqual (new[] { 3, 4 }, backend.Get<int[]> (Settings.Preferences.ExpandedTags));
		}
	}
}
