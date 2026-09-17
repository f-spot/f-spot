// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Gtk.Tests
{
	// Mono.Addins matches XML attribute names to [NodeAttribute] exactly, including
	// case. A required attribute the XML spells differently is not "optional" -- the
	// node fails to load, the menu item or editor never appears, and the log fills
	// with "Required attribute '…' not found."
	[TestClass]
	public class AddinXmlAttributeTests
	{
		[TestMethod]
		public void CommandElements_CarryTheAttributeCommandMenuItemNodeReads ()
		{
			AssertXmlElementsSupply ("Command", RequiredAttributeName (
				Path.Combine (GtkClient (), "FSpot.Extensions", "CommandMenuItemNode.cs")));
		}

		[TestMethod]
		public void EditorElements_CarryTheAttributeEditorNodeReads ()
		{
			AssertXmlElementsSupply ("Editor", RequiredAttributeName (
				Path.Combine (GtkClient (), "FSpot.Editors", "EditorNode.cs")));
		}

		static void AssertXmlElementsSupply (string elementName, string attributeName)
		{
			var missing = AddinDocuments ()
				.SelectMany (item => item.document.Descendants (elementName)
					.Where (element => element.Attribute (attributeName) == null)
					.Select (element => $"{item.file}: <{elementName}> missing '{attributeName}'"))
				.ToList ();

			Assert.IsEmpty (missing,
				$"{elementName} elements must set '{attributeName}' (the [NodeAttribute] the node type reads): "
				+ string.Join ("; ", missing));
		}

		static string RequiredAttributeName (string sourcePath)
		{
			string source = File.ReadAllText (sourcePath);

			var named = Regex.Match (source, @"\[NodeAttribute\s*\(\s*""([^""]+)""");
			if (named.Success)
				return named.Groups[1].Value;

			var field = Regex.Match (source,
				@"\[NodeAttribute\s*\([^]]*Required\s*=\s*true[^]]*\)\]\s*protected\s+string\s+(\w+)");
			Assert.IsTrue (field.Success, $"{sourcePath} has no required [NodeAttribute] to pin against the XML");
			return field.Groups[1].Value;
		}

		static IEnumerable<(string file, XDocument document)> AddinDocuments ()
		{
			string root = RepoRoot ();
			var files = Directory.GetFiles (Path.Combine (root, "src"), "*.addin.xml", SearchOption.AllDirectories);
			Assert.IsNotEmpty (files, "no addin XML under src/; the search root is wrong");
			return files.Select (file => (file, XDocument.Load (file)));
		}

		static string GtkClient () => Path.Combine (RepoRoot (), "src", "Clients", "FSpot.Gtk");

		static string RepoRoot ()
		{
			for (var dir = new DirectoryInfo (AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
				if (File.Exists (Path.Combine (dir.FullName, "F-Spot.slnx")))
					return dir.FullName;
			}

			Assert.Fail ("F-Spot.slnx not found walking up from " + AppContext.BaseDirectory);
			return null;
		}
	}
}
