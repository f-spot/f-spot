// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class AxamlMarkupTests
{
	static readonly Regex AttributeAssignment = new (
		@"[\w.]+\s*=\s*[""']",
		RegexOptions.CultureInvariant);

	[UiTestMethod]
	public void StartTags_DoNotCloseBeforeTheirAttributes ()
	{
		var leaks = AxamlDocuments ()
			.SelectMany (item => item.document.Descendants ()
				.Select (element => (item.file, element, text: DirectText (element)))
				.Where (hit => AttributeAssignment.IsMatch (hit.text))
				.Select (hit => $"{Relative (hit.file)} <{LocalName (hit.element)}> leaked attribute text: {Truncate (hit.text)}"))
			.ToList ();

		Assert.IsEmpty (leaks,
			"A '>' closed the start tag before its attributes; Avalonia then shows that text instead of the bound items: "
			+ string.Join ("; ", leaks));
	}

	static string DirectText (XElement element) =>
		string.Concat (element.Nodes ().OfType<XText> ().Select (node => node.Value)).Trim ();

	static IEnumerable<(string file, XDocument document)> AxamlDocuments ()
	{
		string root = Path.Combine (RepoRoot (), "src", "Clients", "FSpot.Avalonia");
		var files = Directory.GetFiles (root, "*.axaml", SearchOption.AllDirectories);
		Assert.IsNotEmpty (files, "no .axaml under FSpot.Avalonia; the search root is wrong");
		return files.Select (file => (file, XDocument.Load (file)));
	}

	static string RepoRoot ()
	{
		for (var dir = new DirectoryInfo (AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
			if (File.Exists (Path.Combine (dir.FullName, "F-Spot.slnx")))
				return dir.FullName;
		}

		Assert.Fail ("F-Spot.slnx not found walking up from " + AppContext.BaseDirectory);
		return null!;
	}

	static string Relative (string file) =>
		Path.GetRelativePath (RepoRoot (), file);

	static string LocalName (XElement element) =>
		element.Name.LocalName;

	static string Truncate (string text) =>
		text.Length <= 80 ? text : text[..80] + "…";
}
