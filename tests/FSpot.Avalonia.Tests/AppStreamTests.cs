// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class AppStreamTests
{
	[TestMethod]
	public void Metainfo_LaunchesTheDesktopFileAndNamesTheLargeIcon ()
	{
		string root = RepoPaths.Root ();
		var meta = XDocument.Load (Path.Combine (root, "data", "app.f-spot.FSpot.metainfo.xml"));
		var component = meta.Root;
		Assert.IsNotNull (component);
		Assert.AreEqual ("component", component.Name.LocalName);
		Assert.AreEqual ("desktop-application", (string?)component.Attribute ("type"));
		Assert.AreEqual ("app.f-spot.FSpot", Text (component, "id"));
		Assert.AreEqual ("MIT", Text (component, "project_license"));
		Assert.AreEqual ("F-Spot", Text (component, "name"));
		Assert.IsFalse (string.IsNullOrWhiteSpace (Text (component, "summary")));
		Assert.IsNotNull (component.Element ("description")?.Element ("p"));

		var launchable = component.Element ("launchable");
		Assert.IsNotNull (launchable);
		Assert.AreEqual ("desktop-id", (string?)launchable.Attribute ("type"));
		Assert.AreEqual ("app.f-spot.FSpot.desktop", launchable.Value);
		Assert.IsTrue (File.Exists (Path.Combine (root, "data", launchable.Value)));

		var homepage = component.Elements ("url").Single (url => (string?)url.Attribute ("type") == "homepage");
		Assert.AreEqual ("https://f-spot.app", homepage.Value);

		var icon = component.Elements ("icon").Single (item => (string?)item.Attribute ("type") == "cached");
		Assert.AreEqual ("256", (string?)icon.Attribute ("width"));
		Assert.AreEqual ("256", (string?)icon.Attribute ("height"));
		Assert.AreEqual ("f-spot", icon.Value);

		string desktop = File.ReadAllText (Path.Combine (root, "data", launchable.Value));
		Assert.Contains ("Type=Application", desktop);
		Assert.Contains ("Name=F-Spot", desktop);
		Assert.Contains ("Icon=f-spot", desktop);
		Assert.Contains ("Exec=FSpot.Avalonia", desktop);
	}

	static string Text (XElement parent, string name)
		=> parent.Element (name)?.Value ?? string.Empty;
}
