// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using FSpot.Database;
using FSpot.Imaging;
using FSpot.Platform;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.Thumbnail;

using Moq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Sentry;
using Sentry.Protocol;

namespace FSpot.Tests
{
	[TestClass]
	public class FSpotSentryTests
	{
		string settingsFile;
		string previousOverride;
		string previousKillSwitch;
		Func<System.Collections.Generic.IEnumerable<string>> previousTags;

		[TestInitialize]
		public void Isolate ()
		{
			previousOverride = PreferenceJsonBackend.PreferenceLocationOverride;
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
			FSpotSentry.UseTagCatalog (null);
			PreferenceJsonBackend.PreferenceLocationOverride = previousOverride;
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
		public void ReportingDescription_FollowsThePreferenceAndTheKillSwitch ()
		{
			Assert.AreEqual (Strings.CrashReportsStayOffUntilEnabled, FSpotSentry.ReportingDescription ());

			LibraryPreferences.SendCrashReports = false;
			Assert.AreEqual (Strings.CrashReportsAreOff, FSpotSentry.ReportingDescription ());

			LibraryPreferences.SendCrashReports = true;
			Environment.SetEnvironmentVariable (FSpotSentry.KillSwitchVariable, "0");
			Assert.AreEqual (Strings.CrashReportsAreOff, FSpotSentry.ReportingDescription ());
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

		[TestMethod]
		public void Scrub_ReplacesQuotedPathsAndSpacedFilenames ()
		{
			string quoted = "Could not find file '/Volumes/PhotoDisk/Family/Carol Jones.jpg'.";
			string denied = "Access to the path '/Volumes/Photo Disk/Family/x.jpg' is denied.";
			string spacedName = "/Volumes/Photo Disk/Family/Holiday at Grandmas house.jpg";
			string text = $"{quoted} {denied} open {spacedName} now";

			string scrubbed = FSpotSentry.Scrub (text);

			Assert.DoesNotContain ("PhotoDisk", scrubbed);
			Assert.DoesNotContain ("Carol Jones", scrubbed);
			Assert.DoesNotContain ("Grandmas", scrubbed);
			Assert.DoesNotContain ("house.jpg", scrubbed);
			Assert.Contains ("Could not find file '", scrubbed);
			Assert.Contains ("' is denied.", scrubbed);
			Assert.Contains (" now", scrubbed);
			Assert.Contains ("[path]", scrubbed);
		}

		[TestMethod]
		public void Scrub_MatchesTagsOnWordBoundariesIgnoringCase ()
		{
			FSpotSentry.TagNames = () => new[] { "Beach" };
			string scrubbed = FSpotSentry.Scrub ("at beach today, not Beaches");

			Assert.Contains ("[tag]", scrubbed);
			Assert.DoesNotContain ("beach", scrubbed);
			Assert.Contains ("Beaches", scrubbed);
		}

		[TestMethod]
		public void ScrubEvent_RendersStructuredValuesBeforeScrubbing ()
		{
			string secret = "/Volumes/PhotoDisk/Family/Carol Jones.jpg";
			var evt = new SentryEvent {
				Message = new SentryMessage { Formatted = "kept" }
			};
			evt.SetExtra ("detail", new QuotedPath (secret));
			evt.SetExtra ("link", new Uri ("file://" + secret));

			SentryEvent scrubbed = FSpotSentry.ScrubEvent (evt);

			Assert.DoesNotContain ("Carol Jones", scrubbed.Extra["detail"].ToString ());
			Assert.DoesNotContain ("PhotoDisk", scrubbed.Extra["link"].ToString ());
			Assert.DoesNotContain (secret, scrubbed.Extra["link"].ToString ());
		}

		[TestMethod]
		public async Task SerilogSink_ScrubsQuotedPathsOutOfTheEnvelope ()
		{
			string secret = "/Volumes/PhotoDisk/Family/Carol Jones.jpg";
			string logFile = Path.Combine (Path.GetTempPath (), "fspot-sentry-log-" + Guid.NewGuid ().ToString ("n"));
			var transport = new CapturingTransport ();
			try {
				using (SentrySdk.Init (options => {
					FSpotSentry.Configure (options, FSpotSentry.AvaloniaClient);
					options.Transport = transport;
				})) {
					Serilog.ILogger logger = Logger.BuildLogger (logFile);
					logger.Error (
						new FileNotFoundException ("Could not find file '" + secret + "'."),
						"load failed");
					(logger as IDisposable)?.Dispose ();
					await SentrySdk.FlushAsync (TimeSpan.FromSeconds (5));
				}

				Assert.IsGreaterThan (0, transport.Body.Length);
				Assert.DoesNotContain (secret, transport.Body);
				Assert.DoesNotContain ("Carol Jones", transport.Body);
				Assert.DoesNotContain ("PhotoDisk", transport.Body);
			} finally {
				if (File.Exists (logFile))
					File.Delete (logFile);
			}
		}

		sealed class QuotedPath
		{
			readonly string path;
			public QuotedPath (string path) => this.path = path;
			public override string ToString () => "Could not find file '" + path + "'.";
		}

		sealed class CapturingTransport : Sentry.Extensibility.ITransport
		{
			public string Body { get; private set; } = "";

			public async System.Threading.Tasks.Task SendEnvelopeAsync (Sentry.Protocol.Envelopes.Envelope envelope, System.Threading.CancellationToken cancellationToken = default)
			{
				using var buffer = new MemoryStream ();
				await envelope.SerializeAsync (buffer, null, cancellationToken);
				Body += System.Text.Encoding.UTF8.GetString (buffer.ToArray ());
			}
		}

		[TestMethod]
		public void Scrub_ReplacesSpacedHfsUncAndFileUrisAndKeepsTheTrailingWord ()
		{
			string spaced = "/Volumes/Photo Disk/Family/Bob.jpg";
			string hfs = "Macintosh HD:Users:alice:Photos:Bob.jpg";
			string unc = @"\\server\share\My Photos\Bob.jpg";
			string fileUri = "file:///Volumes/Photo Disk/Family/Bob.jpg";
			string drive = @"D:\Users\carol\My Photos\Bob.jpg";
			string text = $"open {spaced} now then {hfs} and {unc} and {fileUri} and {drive}";

			string scrubbed = FSpotSentry.Scrub (text);

			Assert.DoesNotContain ("Bob.jpg", scrubbed);
			Assert.DoesNotContain ("Photo Disk", scrubbed);
			Assert.DoesNotContain ("alice", scrubbed);
			Assert.DoesNotContain ("carol", scrubbed);
			Assert.DoesNotContain (@"\\server", scrubbed);
			Assert.DoesNotContain ("file://", scrubbed);
			Assert.Contains ("open ", scrubbed);
			Assert.Contains (" now ", scrubbed);
			Assert.Contains (" then ", scrubbed);
			Assert.Contains ("[path]", scrubbed);
		}

		[TestMethod]
		public void ScrubEvent_ScrubsExtrasContextsAndBreadcrumbData ()
		{
			string spaced = "/Volumes/Photo Disk/Family/Bob.jpg";
			var evt = new SentryEvent {
				Message = new SentryMessage { Formatted = "kept" }
			};
			evt.SetExtra ("Path", spaced);
			evt.SetExtra ("note", $"see {spaced} now");
			evt.SetExtra ("profile", "left alone");
			evt.Contexts["os"] = "file:///Volumes/Photo Disk/Family/Bob.jpg";
			evt.Contexts["device"] = new Dictionary<string, object> {
				["folder"] = @"\\server\share\My Photos\Bob.jpg",
				["name"] = "camera"
			};

			SentryEvent scrubbed = FSpotSentry.ScrubEvent (evt);

			Assert.AreEqual ("[path]", scrubbed.Extra["Path"]);
			Assert.DoesNotContain ("Bob.jpg", (string)scrubbed.Extra["note"]);
			Assert.Contains (" now", (string)scrubbed.Extra["note"]);
			Assert.AreEqual ("left alone", scrubbed.Extra["profile"]);
			Assert.DoesNotContain ("Bob.jpg", (string)scrubbed.Contexts["os"]);
			Assert.DoesNotContain ("file://", (string)scrubbed.Contexts["os"]);
			var device = (Dictionary<string, object>)scrubbed.Contexts["device"];
			Assert.AreEqual ("[path]", device["folder"]);
			Assert.AreEqual ("camera", device["name"]);

			var data = new Dictionary<string, string> {
				["exception_message"] = $"failed {spaced} now",
				["filename"] = spaced
			};
			Breadcrumb crumb = FSpotSentry.ScrubBreadcrumb (
				new Breadcrumb ("see " + spaced, "default", data, "log", BreadcrumbLevel.Info));
			Assert.DoesNotContain ("Bob.jpg", crumb.Message);
			Assert.DoesNotContain ("Bob.jpg", crumb.Data["exception_message"]);
			Assert.Contains (" now", crumb.Data["exception_message"]);
			Assert.AreEqual ("[path]", crumb.Data["filename"]);
		}

		[TestMethod]
		public void ScrubEvent_ScrubsStackFramePathsOnExceptionsAndThreads ()
		{
			string spaced = "/Volumes/Photo Disk/Family/Bob.jpg";
			string hfs = "Macintosh HD:Users:alice:Photos:Bob.jpg";
			string drive = @"D:\Users\carol\My Photos\Bob.jpg";
			var evt = new SentryEvent {
				SentryExceptions = new[] {
					new SentryException {
						Value = "failed " + spaced + " now",
						Stacktrace = new SentryStackTrace {
							Frames = new[] {
								new SentryStackFrame {
									FileName = spaced,
									AbsolutePath = hfs,
									Module = drive
								}
							}
						}
					}
				},
				SentryThreads = new[] {
					new SentryThread {
						Stacktrace = new SentryStackTrace {
							Frames = new[] { new SentryStackFrame { AbsolutePath = spaced } }
						}
					}
				}
			};

			SentryEvent scrubbed = FSpotSentry.ScrubEvent (evt);
			SentryStackFrame exceptionFrame = scrubbed.SentryExceptions.Single ().Stacktrace.Frames.Single ();
			Assert.DoesNotContain ("Bob.jpg", exceptionFrame.FileName);
			Assert.DoesNotContain ("alice", exceptionFrame.AbsolutePath);
			Assert.DoesNotContain ("carol", exceptionFrame.Module);
			Assert.Contains (" now", scrubbed.SentryExceptions.Single ().Value);
			Assert.DoesNotContain ("Bob.jpg", scrubbed.SentryThreads.Single ().Stacktrace.Frames.Single ().AbsolutePath);
		}

		[TestMethod]
		public void Scrub_UsesTheTagCatalogRatherThanAReplacedDelegate ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-tags-" + Guid.NewGuid ().ToString ("n"));
			Directory.CreateDirectory (root);
			const string tagName = "Alice Shore";
			Db db = null;
			try {
				db = new Db (new Mock<IImageFileFactory> ().Object, new Mock<IThumbnailService> ().Object, new Mock<IUpdaterUI> ().Object);
				db.Init (Path.Combine (root, "photos.db"), true);
				db.Tags.CreateTag (db.Tags.RootCategory, tagName, false);
				FSpotSentry.UseTagCatalog (db.Tags);

				string scrubbed = FSpotSentry.Scrub ($"met {tagName} near /Volumes/Photo Disk/Family/Bob.jpg now");

				Assert.DoesNotContain (tagName, scrubbed);
				Assert.Contains ("[tag]", scrubbed);
				Assert.DoesNotContain ("Bob.jpg", scrubbed);
				Assert.Contains (" now", scrubbed);
			} finally {
				FSpotSentry.UseTagCatalog (null);
				db?.Dispose ();
				if (Directory.Exists (root))
					Directory.Delete (root, true);
			}
		}
	}
}
