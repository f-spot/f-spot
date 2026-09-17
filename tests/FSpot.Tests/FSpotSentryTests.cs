// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Platform;
using FSpot.Settings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Sentry;

namespace FSpot.Tests
{
	[TestClass]
	public class FSpotSentryTests
	{
		string settingsFile;
		string previousKillSwitch;
		Func<System.Collections.Generic.IEnumerable<string>> previousTags;

		[TestInitialize]
		public void Isolate ()
		{
			settingsFile = Path.Combine (Path.GetTempPath (), "f-spot-sentry-" + Guid.NewGuid ().ToString ("n") + ".json");
			PreferenceJsonBackend.PreferenceLocationOverride = settingsFile;
			PreferenceJsonBackend.Unload ();
			previousKillSwitch = Environment.GetEnvironmentVariable (FSpotSentry.KillSwitchVariable);
			Environment.SetEnvironmentVariable (FSpotSentry.KillSwitchVariable, null);
			previousTags = FSpotSentry.TagNames;
		}

		[TestCleanup]
		public void Restore ()
		{
			Environment.SetEnvironmentVariable (FSpotSentry.KillSwitchVariable, previousKillSwitch);
			FSpotSentry.TagNames = previousTags;
			PreferenceJsonBackend.PreferenceLocationOverride = null;
			PreferenceJsonBackend.Unload ();
			if (File.Exists (settingsFile))
				File.Delete (settingsFile);
		}

		[TestMethod]
		public void ReportingIsOnUntilThePreferenceIsCleared ()
		{
			Assert.IsTrue (LibraryPreferences.SendCrashReports);
			Assert.IsTrue (FSpotSentry.IsRequested ());

			LibraryPreferences.SendCrashReports = false;
			Assert.IsFalse (FSpotSentry.IsRequested ());
		}

		[TestMethod]
		public void KillSwitchForcesReportingOff ()
		{
			LibraryPreferences.SendCrashReports = true;
			foreach (string off in new[] { "0", "false", "off", "NO" }) {
				Environment.SetEnvironmentVariable (FSpotSentry.KillSwitchVariable, off);
				Assert.IsFalse (FSpotSentry.IsRequested (), off);
			}

			Environment.SetEnvironmentVariable (FSpotSentry.KillSwitchVariable, "1");
			Assert.IsTrue (FSpotSentry.IsRequested ());
			Assert.IsFalse (FSpotSentry.IsKillSwitchOff (null));
			Assert.IsFalse (FSpotSentry.IsKillSwitchOff ("  "));
		}

		[TestMethod]
		public void Configure_SetsTheBuildEnvironmentAndScrubsPathsAndTagNames ()
		{
			var options = new SentryOptions ();
			FSpotSentry.Configure (options, FSpotSentry.AvaloniaClient);

			Assert.AreEqual (FSpotSentry.EnvironmentName, options.Environment);
			Assert.AreEqual ("avalonia", options.DefaultTags["client"]);
			Assert.IsFalse (options.SendDefaultPii);

			string home = FSpotConfiguration.HomeDirectory;
			FSpotSentry.TagNames = () => new[] { "Beach" };
			var evt = new SentryEvent {
				Message = new SentryMessage {
					Formatted = $"open {home}/Photos/a.jpg tag Beach then /var/tmp/secret.jpg"
				}
			};
			SentryEvent scrubbed = FSpotSentry.ScrubEvent (evt);
			Assert.DoesNotContain (home, scrubbed.Message.Formatted);
			Assert.DoesNotContain ("Beach", scrubbed.Message.Formatted);
			Assert.DoesNotContain ("/var/tmp/secret.jpg", scrubbed.Message.Formatted);
			Assert.Contains ("[tag]", scrubbed.Message.Formatted);
			Assert.Contains ("[path]", scrubbed.Message.Formatted);

			var crumb = new Breadcrumb (home + "/library and Beach", "default", null, "log", BreadcrumbLevel.Info);
			Breadcrumb scrubbedCrumb = FSpotSentry.ScrubBreadcrumb (crumb);
			Assert.DoesNotContain (home, scrubbedCrumb.Message);
			Assert.DoesNotContain ("Beach", scrubbedCrumb.Message);
		}
	}
}
