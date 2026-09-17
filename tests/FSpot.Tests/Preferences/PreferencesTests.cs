//
// PreferencesTests.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2019 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

using FSpot.Platform;
using FSpot.Tests;

using Newtonsoft.Json.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FSpot.Preferences.Tests
{
	[TestClass]
	public class PreferencesTests
	{
		string TestSettingsFile;
		PreferenceJsonBackend jsonBackend;

		JObject LoadSettings (string location)
		{
			var settingsFile = File.ReadAllText (location);
			var o = JObject.Parse (settingsFile);
			return (JObject)o[PreferenceJsonBackend.SettingsRoot];
		}

		[TestInitialize]
		public void Setup ()
		{
			var tmpFile = Path.GetTempFileName ();
			var jsonfile = Path.ChangeExtension (tmpFile, "json");
			File.Move (tmpFile, jsonfile);
			PreferenceJsonBackend.PreferenceLocationOverride = TestSettingsFile = jsonfile;
			jsonBackend = new PreferenceJsonBackend ();
		}

		[TestCleanup]
		public void TearDown ()
		{
			if (File.Exists (TestSettingsFile))
				File.Delete (TestSettingsFile);
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
			Assert.AreEqual (v, LoadSettings (TestSettingsFile)[key].ToObject<string> ());
		}

		[TestMethod]
		public void CanSetBoolSetting ()
		{
			const string key = Settings.Preferences.ExportKey;
			jsonBackend.Set (key, true);
			jsonBackend.SaveSettings ();
			AssertSettingsFileWritten ();
			Assert.IsTrue (LoadSettings (TestSettingsFile)[key].ToObject<bool> ());
		}

		[TestMethod]
		public void CanSetDoubleSetting ()
		{
			const string key = Settings.Preferences.CustomCropRatios;
			jsonBackend.Set (key, 0.0);
			jsonBackend.SaveSettings ();
			AssertSettingsFileWritten ();
			Assert.AreEqual (0.0, LoadSettings (TestSettingsFile)[key].ToObject<double> ());
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
			var result = settings[key].ToObject<List<int>> ();

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
		public void ResetIncorrectTypeInSettings ()
		{
			Settings.Preferences.Set ("RandomKey", "WrongValue");
			var result = Settings.Preferences.TryGet ("RandomKey", out double randomValue);

			Assert.IsTrue (result);
			Assert.AreEqual (0.0, randomValue);
		}
	}
}
