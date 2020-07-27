//
// PreferencesTests.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2019 Stephen Shaw
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
using System.IO;

using NUnit.Framework;

using Newtonsoft.Json.Linq;

using FSpot.Platform;
using Shouldly;

namespace FSpot.Preferences.UnitTest
{
	[TestFixture]
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

		[SetUp]
		public void Setup ()
		{
			var tmpFile = Path.GetTempFileName ();
			var jsonfile = Path.ChangeExtension (tmpFile, "json");
			File.Move (tmpFile, jsonfile);
			PreferenceJsonBackend.PreferenceLocationOverride = TestSettingsFile = jsonfile;
			jsonBackend = new PreferenceJsonBackend ();
		}

		[TearDown]
		public void TearDown ()
		{
			if (File.Exists (TestSettingsFile))
				File.Delete (TestSettingsFile);
		}

		[TestCase (Settings.Preferences.StoragePath, "StoragePathString")]
		[TestCase (Settings.Preferences.ExportKey, true)]
		[TestCase (Settings.Preferences.CustomCropRatios, 0.0)]
		public void CanSetSetting (string key, object v)
		{
			jsonBackend.Set (key, v);
			jsonBackend.SaveSettings ();
			Assert.That (new FileInfo (TestSettingsFile).Length > 0);

			var settings = LoadSettings (TestSettingsFile);
			var result = settings[key].ToString ();

			Assert.AreEqual (v.ToString (), result);
		}

		[TestCase (Settings.Preferences.StoragePath, "StoragePathString")]
		public void CanGetStringSettings (string key, string v)
		{
			jsonBackend.Set (key, v);
			jsonBackend.SaveSettings ();
			Assert.That (new FileInfo (TestSettingsFile).Length > 0);

			var result = jsonBackend.Get<string> (key);

			Assert.AreEqual (v, result);
		}

		[TestCase (Settings.Preferences.ExportKey, true)]
		public void CanGetBoolSettings (string key, bool v)
		{
			jsonBackend.Set (key, v);
			jsonBackend.SaveSettings ();
			Assert.That (new FileInfo (TestSettingsFile).Length > 0);

			var result = jsonBackend.Get<bool> (key);

			Assert.AreEqual (v, result);
		}


		[TestCase (Settings.Preferences.CustomCropRatios, 0.0)]
		public void CanGetDoubleSettings (string key, double v)
		{
			jsonBackend.Set (key, v);
			jsonBackend.SaveSettings ();
			Assert.That (new FileInfo (TestSettingsFile).Length > 0);

			var result = jsonBackend.Get<double> (key);

			Assert.AreEqual (v, result);
		}

		[Test]
		public void UnsetSettingThrowsNoSuchKeyException ()
		{
			Assert.Throws<NoSuchKeyException> (() => jsonBackend.Get<bool> ("RandomKey123"));
		}

		// Preferences returns the default value instead of an exception
		[Test]
		public void PreferencesGetDefaultSetting ()
		{
			var result = Settings.Preferences.Get<bool> (Settings.Preferences.TagIconAutomatic);
			Assert.True (result);
		}

		[Test]
		public void ResetIncorrectTypeInSettings ()
		{
			Settings.Preferences.Set ("RandomKey", null);
			var result = Settings.Preferences.TryGet ("RandomKey", out double randomValue);

			result.ShouldBeTrue();
			randomValue.ShouldBe (default);
		}
	}
}
