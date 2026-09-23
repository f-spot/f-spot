// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Query.Tests
{
	[TestClass]
	public sealed class FolderTreeTests
	{
		[TestMethod]
		public void Build_Empty_ReturnsNoNodes ()
		{
			Assert.IsEmpty (FolderTree.Build (System.Array.Empty<FolderCount> ()));
		}

		[TestMethod]
		public void Build_SingleFolder_NestsSchemeAndPathSegments ()
		{
			var tree = FolderTree.Build (new[] {
				new FolderCount (new SafeUri ("file:///photos/2020/01/", true), 3)
			});

			Assert.HasCount (1, tree);
			var photos = tree[0];
			Assert.AreEqual ("photos", photos.Name);
			Assert.AreEqual (3, photos.Count);

			var year = photos.Children.Single ();
			Assert.AreEqual ("2020", year.Name);
			Assert.AreEqual (3, year.Count);

			var month = year.Children.Single ();
			Assert.AreEqual ("01", month.Name);
			Assert.AreEqual (3, month.Count);
			Assert.IsEmpty (month.Children);
			Assert.AreEqual (new SafeUri ("file:///photos/2020/01/", true), month.Uri);
		}

		[TestMethod]
		public void Build_SharedPrefix_SumsCountsOnParents ()
		{
			var tree = FolderTree.Build (new[] {
				new FolderCount (new SafeUri ("file:///photos/2020/01/", true), 2),
				new FolderCount (new SafeUri ("file:///photos/2020/06/", true), 1)
			});

			Assert.AreEqual (3, tree[0].Count);
			var year = tree[0].Children.Single ();
			Assert.AreEqual ("2020", year.Name);
			Assert.AreEqual (3, year.Count);
			Assert.HasCount (2, year.Children);
			Assert.AreEqual ("01", year.Children[0].Name);
			Assert.AreEqual (2, year.Children[0].Count);
			Assert.AreEqual ("06", year.Children[1].Name);
			Assert.AreEqual (1, year.Children[1].Count);
		}

		[TestMethod]
		public void Build_UnescapesSegmentNames ()
		{
			var tree = FolderTree.Build (new[] {
				new FolderCount (new SafeUri ("file:///My%20Photos/", true), 1)
			});

			Assert.AreEqual ("My Photos", tree[0].Name);
		}

		[TestMethod]
		public void Build_DropsADayThatHasNoPhotos ()
		{
			var tree = FolderTree.Build (new[] {
				new FolderCount (new SafeUri ("file:///photos/2020/01/", true), 0),
				new FolderCount (new SafeUri ("file:///photos/2020/06/", true), 2)
			});

			var year = tree[0].Children.Single ();
			Assert.AreEqual ("2020", year.Name);
			Assert.HasCount (1, year.Children);
			Assert.AreEqual ("06", year.Children[0].Name);
			Assert.AreEqual (2, year.Children[0].Count);
		}
	}
}
