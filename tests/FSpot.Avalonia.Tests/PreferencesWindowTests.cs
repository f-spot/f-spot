// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;

using FSpot;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class GtkMarkupTextTests
{
	[TestMethod]
	public void Split_SeparatesTheGtkSmallCaption ()
	{
		var (title, detail) = GtkMarkupText.Split (Strings.InsideTheImageFilesWhenPossibleSmallMakesThemAccessibleToOtherImageEditingProgramsSmall);

		Assert.AreEqual ("Inside the image files when possible", title);
		Assert.AreEqual ("Makes them accessible to other image-editing programs.", detail);
	}

	[TestMethod]
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
	[TestMethod]
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
				Assert.AreEqual (false, window.EmbedMetadata.IsChecked);
				Assert.AreEqual (true, window.SeparateMetadata.IsChecked);
				Assert.AreEqual (true, window.AlwaysSidecar.IsChecked);
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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

	[TestMethod]
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
}

[TestClass]
public sealed class SystemColorProfileTests
{
	[TestMethod]
	public void Current_NeverThrowsAndDegradesWhenUnavailable ()
	{
		var profile = SystemColorProfile.Current;
		Assert.AreEqual (profile is not null, SystemColorProfile.IsAvailable);
		if (!OperatingSystem.IsMacOS ())
			Assert.IsNull (profile);
	}
}
