// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using FSpot.Extensions;

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
			AssertXmlElementsSupply ("Command", AddinAttributeNames.CommandType);
		}

		[TestMethod]
		public void EditorElements_CarryTheAttributeEditorNodeReads ()
		{
			AssertXmlElementsSupply ("Editor", AddinAttributeNames.EditorType);
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

		static IEnumerable<(string file, XDocument document)> AddinDocuments ()
		{
			string root = RepoPaths.Root ();
			var files = Directory.GetFiles (Path.Combine (root, "src"), "*.addin.xml", SearchOption.AllDirectories);
			Assert.IsNotEmpty (files, "no addin XML under src/; the search root is wrong");
			return files.Select (file => (file, XDocument.Load (file)));
		}
	}
}
