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
}
