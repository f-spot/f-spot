// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;

using FSpot;
using FSpot.Cms;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Sentry;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class GtkMarkupTextTests
{
	[UiTestMethod]
	public void Split_SeparatesTheGtkSmallCaption ()
	{
		var (title, detail) = GtkMarkupText.Split (Strings.InsideTheImageFilesWhenPossibleSmallMakesThemAccessibleToOtherImageEditingProgramsSmall);

		Assert.AreEqual ("Inside the image files when possible", title);
		Assert.AreEqual ("Makes them accessible to other image-editing programs.", detail);
	}

	[UiTestMethod]
	public void Split_KeepsAPlainSentenceWhole ()
	{
		var (title, detail) = GtkMarkupText.Split (Strings.SeparatelyFromTheImageFiles);

		Assert.AreEqual ("Separately from the image files", title);
		Assert.AreEqual (string.Empty, detail);
	}
}

[TestClass]
[DoNotParallelize]
public sealed class PreferencesWindowTests
{
	[UiTestMethod]
	public void MetadataRows_FollowTheGtkEmbedAndSidecarRules ()
	{
		AvaloniaApp.Ensure ();
		bool embed = LibraryPreferences.MetadataEmbedInImage;
		bool sidecar = LibraryPreferences.MetadataAlwaysUseSidecar;
		string display = LibraryPreferences.DisplayProfile;
		string print = LibraryPreferences.PrintProfile;
		string scheme = LibraryPreferences.ColorScheme;
		bool interpolation = LibraryPreferences.ViewerInterpolation;
		string storage = LibraryPreferences.StoragePath;
		try {
			LibraryPreferences.MetadataEmbedInImage = false;
			LibraryPreferences.MetadataAlwaysUseSidecar = true;
			var window = new PreferencesWindow ();
			window.Show ();
			try {
				Assert.IsFalse (window.EmbedMetadata.IsChecked);
				Assert.IsTrue (window.SeparateMetadata.IsChecked);
				Assert.IsTrue (window.AlwaysSidecar.IsChecked);
				Assert.IsFalse (window.AlwaysSidecar.IsEnabled);
				Assert.Contains (Strings.None, window.DisplayProfile.Items.Cast<object> ());
				Assert.Contains (Strings.None, window.PrintProfile.Items.Cast<object> ());

				window.EmbedMetadata.IsChecked = true;
				Assert.IsTrue (window.AlwaysSidecar.IsEnabled);
			} finally {
				window.Close ();
			}

			Assert.IsTrue (LibraryPreferences.MetadataEmbedInImage);
			Assert.IsTrue (LibraryPreferences.MetadataAlwaysUseSidecar);
		} finally {
			LibraryPreferences.MetadataEmbedInImage = embed;
			LibraryPreferences.MetadataAlwaysUseSidecar = sidecar;
			LibraryPreferences.DisplayProfile = display;
			LibraryPreferences.PrintProfile = print;
			LibraryPreferences.ColorScheme = scheme;
			LibraryPreferences.ViewerInterpolation = interpolation;
			LibraryPreferences.StoragePath = storage;
		}
	}

	[UiTestMethod]
	public void DisplayProfileCombo_OffersSystemProfileOnlyForDisplayWhenAvailable ()
	{
		AvaloniaApp.Ensure ();
		string display = LibraryPreferences.DisplayProfile;
		string print = LibraryPreferences.PrintProfile;
		try {
			LibraryPreferences.DisplayProfile = string.Empty;
			LibraryPreferences.PrintProfile = string.Empty;
			var window = new PreferencesWindow ();
			window.Show ();
			try {
				bool systemProfileOffered = window.DisplayProfile.Items.Cast<object> ().Contains (Strings.SystemProfile);
				Assert.AreEqual (SystemColorProfile.IsAvailable, systemProfileOffered);
				Assert.DoesNotContain (Strings.SystemProfile, window.PrintProfile.Items.Cast<object> (),
					"the print profile combo has no system profile in FSpot.Gtk either");
			} finally {
				window.Close ();
			}
		} finally {
			LibraryPreferences.DisplayProfile = display;
			LibraryPreferences.PrintProfile = print;
		}
	}

	[UiTestMethod]
	public void DisplayProfileCombo_RoundTripsTheSystemProfileSentinel ()
	{
		if (!SystemColorProfile.IsAvailable)
			Assert.Inconclusive ("No system display profile is available in this environment.");

		AvaloniaApp.Ensure ();
		string display = LibraryPreferences.DisplayProfile;
		try {
			LibraryPreferences.DisplayProfile = SystemColorProfile.Sentinel;
			var window = new PreferencesWindow ();
			window.Show ();
			try {
				Assert.AreEqual (Strings.SystemProfile, window.DisplayProfile.SelectedItem);
			} finally {
				window.Close ();
			}

			Assert.AreEqual (SystemColorProfile.Sentinel, LibraryPreferences.DisplayProfile);
		} finally {
			LibraryPreferences.DisplayProfile = display;
		}
	}

	[UiTestMethod]
	public void TransparencyRows_FollowTheGtkRulesAndPersist ()
	{
		AvaloniaApp.Ensure ();
		string transparency = LibraryPreferences.ViewerTransparency;
		string transColor = LibraryPreferences.ViewerTransColor;
		try {
			LibraryPreferences.ViewerTransparency = "CHECK_PATTERN";
			LibraryPreferences.ViewerTransColor = "#112233";
			var window = new PreferencesWindow ();
			window.Show ();
			try {
				Assert.IsTrue (window.TransparencyCheckPattern.IsChecked);
				Assert.IsFalse (window.TransparencyColorHex.IsEnabled, "the hex box only matters for 'As custom color'");

				window.TransparencyCustomColor.IsChecked = true;
				Assert.IsTrue (window.TransparencyColorHex.IsEnabled);
				window.TransparencyColorHex.Text = "#ff8800";
			} finally {
				window.Close ();
			}

			Assert.AreEqual ("COLOR", LibraryPreferences.ViewerTransparency);
			Assert.AreEqual ("#ff8800", LibraryPreferences.ViewerTransColor);
		} finally {
			LibraryPreferences.ViewerTransparency = transparency;
			LibraryPreferences.ViewerTransColor = transColor;
		}
	}

	[UiTestMethod]
	public void MailEnabledRow_FollowsAndPersistsThePreference ()
	{
		AvaloniaApp.Ensure ();
		bool mailEnabled = LibraryPreferences.MailToEnabled;
		try {
			LibraryPreferences.MailToEnabled = false;
			var window = new PreferencesWindow ();
			window.Show ();
			try {
				Assert.IsFalse (window.MailEnabled.IsChecked);
				window.MailEnabled.IsChecked = true;
			} finally {
				window.Close ();
			}

			Assert.IsTrue (LibraryPreferences.MailToEnabled);
		} finally {
			LibraryPreferences.MailToEnabled = mailEnabled;
		}
	}

	[UiTestMethod]
	public void SendCrashReportsRow_FollowsAndPersistsThePreference ()
	{
		AvaloniaApp.Ensure ();
		bool previous = LibraryPreferences.SendCrashReports;
		string? killSwitch = Environment.GetEnvironmentVariable (FSpotSentry.KillSwitchVariable);
		try {
			Environment.SetEnvironmentVariable (FSpotSentry.KillSwitchVariable, "0");
			LibraryPreferences.SendCrashReports = false;
			var window = new PreferencesWindow ();
			window.Show ();
			try {
				Assert.IsFalse (window.SendCrashReports.IsChecked);
				Assert.AreEqual (Strings.SendCrashReportsToSentry, window.SendCrashReports.Content);
				window.SendCrashReports.IsChecked = true;
			} finally {
				window.Close ();
			}

			Assert.IsTrue (LibraryPreferences.SendCrashReports);
			Assert.IsFalse (SentrySdk.IsEnabled);
		} finally {
			LibraryPreferences.SendCrashReports = previous;
			Environment.SetEnvironmentVariable (FSpotSentry.KillSwitchVariable, killSwitch);
		}
	}
}

[TestClass]
public sealed class SystemColorProfileTests
{
	[UiTestMethod]
	public void Current_NeverThrowsAndDegradesWhenUnavailable ()
	{
		var profile = SystemColorProfile.Current;
		Assert.AreEqual (profile is not null, SystemColorProfile.IsAvailable);
	}

	[TestMethod]
	public void Combine_KeepsARootedPathAndJoinsAFileName ()
	{
		string directory = Path.Combine ("spool", "drivers", "color");
		string rooted = Path.GetFullPath ("display.icc");

		Assert.AreEqual (rooted, SystemColorProfile.Combine (directory, rooted));
		Assert.AreEqual (Path.Combine (directory, "sRGB Color Space Profile.icm"),
			SystemColorProfile.Combine (directory, "sRGB Color Space Profile.icm"));
		Assert.IsNull (SystemColorProfile.Combine (directory, null));
		Assert.IsNull (SystemColorProfile.Combine (directory, ""));
		Assert.IsNull (SystemColorProfile.Combine (directory, "   "));
	}

	[TestMethod]
	public void TryOpen_OpensInjectedBytesAndRejectsGarbage ()
	{
		if (!LcmsProbe.IsAvailable ())
			Assert.Inconclusive ("liblcms2 is not available on this machine");

		using var made = Profile.CreateAlternateRgb ();
		using Profile? opened = SystemColorProfile.TryOpen (Save (made));

		Assert.IsNotNull (opened);
		Assert.AreEqual (IccColorSpace.Rgb, opened.ColorSpace);
		Assert.IsNull (SystemColorProfile.TryOpen (null));
		Assert.IsNull (SystemColorProfile.TryOpen (Array.Empty<byte> ()));
		Assert.IsNull (SystemColorProfile.TryOpen (new byte[] { 1, 2, 3, 4 }));
	}

	static byte[] Save (Profile profile)
	{
		profile.Native.Save (null, out uint size);
		var bytes = new byte[size + 1];
		Assert.IsTrue (profile.Native.Save (bytes, out _));
		int length = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
		return bytes[..length];
	}
}
