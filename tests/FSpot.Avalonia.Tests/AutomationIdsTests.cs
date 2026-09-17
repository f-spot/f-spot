// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;

using FSpot.Editors;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class AutomationIdsTests
{
	[TestMethod]
	public void Catalog_ValuesAreUniqueAndNonEmpty ()
	{
		var values = typeof (AutomationIds)
			.GetFields (BindingFlags.Public | BindingFlags.Static)
			.Where (field => field.IsLiteral && field.FieldType == typeof (string))
			.Select (field => (Name: field.Name, Value: (string)field.GetRawConstantValue ()!))
			.ToList ();

		Assert.IsGreaterThan (20, values.Count);
		foreach (var field in values)
			Assert.IsFalse (string.IsNullOrWhiteSpace (field.Value), field.Name);

		var duplicates = values
			.GroupBy (field => field.Value, StringComparer.Ordinal)
			.Where (group => group.Count () > 1)
			.Select (group => group.Key)
			.ToList ();
		Assert.IsEmpty (duplicates);
	}

	[TestMethod]
	public void SidebarEditConstants_CoverEveryCatalogEditor ()
	{
		var constants = typeof (AutomationIds)
			.GetFields (BindingFlags.Public | BindingFlags.Static)
			.Where (field => field.IsLiteral && field.FieldType == typeof (string))
			.Select (field => (string)field.GetRawConstantValue ()!)
			.Where (value => value.StartsWith ("Main.Sidebar.Edit.", StringComparison.Ordinal))
			.OrderBy (value => value, StringComparer.Ordinal)
			.ToList ();

		var expected = PhotoEditorCatalog.All
			.Select (editor => AutomationIds.SidebarEditEditor (editor.Id))
			.OrderBy (value => value, StringComparer.Ordinal)
			.ToList ();

		Assert.AreSequenceEqual (expected, constants);
	}
}
