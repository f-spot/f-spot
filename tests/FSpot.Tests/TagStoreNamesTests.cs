// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;

using FSpot.Core;
using FSpot.Database;
using FSpot.Imaging;
using FSpot.Platform;
using FSpot.Thumbnail;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace FSpot.Tests
{
	[TestClass]
	public class TagStoreNamesTests
	{
		[TestMethod]
		public void Names_KeepsTheTagThatStayedWhileAnotherTagIsAddedAndRemoved ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-tag-names-" + Guid.NewGuid ().ToString ("n"));
			Directory.CreateDirectory (root);
			Db db = null;
			try {
				db = new Db (new Mock<IImageFileFactory> ().Object, new Mock<IThumbnailService> ().Object, new Mock<IUpdaterUI> ().Object);
				db.Init (Path.Combine (root, "photos.db"), true);
				var kept = db.Tags.CreateTag (db.Tags.RootCategory, "Kept", false);
				for (int i = 0; i < 400; i++)
					db.Tags.CreateTag (db.Tags.RootCategory, "Filler " + i.ToString (), false);

				int thrown = 0;
				int missing = 0;
				int stop = 0;
				var reader = new Thread (() => {
					while (Volatile.Read (ref stop) == 0) {
						try {
							string[] names = db.Tags.Names ();
							if (!Contains (names, "Kept"))
								Interlocked.Increment (ref missing);
							Tag byName = db.Tags.GetTagByName ("Kept");
							if (byName == null || byName.Id != kept.Id)
								Interlocked.Increment (ref missing);
							Tag[] byStart = db.Tags.GetTagsByNameStart ("Kept");
							if (byStart == null || !ContainsTag (byStart, kept.Id))
								Interlocked.Increment (ref missing);
							Tag byId = db.Tags.GetTagById ((int)kept.Id);
							if (byId == null || byId.Id != kept.Id)
								Interlocked.Increment (ref missing);
						} catch (InvalidOperationException) {
							Interlocked.Increment (ref thrown);
						}
					}
				});
				reader.Start ();
				for (int i = 0; i < 80; i++) {
					Tag churn = db.Tags.CreateTag (db.Tags.RootCategory, "Churn " + i.ToString (), false);
					db.Tags.Remove (churn);
				}

				Volatile.Write (ref stop, 1);
				reader.Join ();
				Assert.AreEqual (0, thrown);
				Assert.AreEqual (0, missing);
			} finally {
				db?.Dispose ();
				if (Directory.Exists (root))
					Directory.Delete (root, true);
			}
		}

		static bool Contains (string[] names, string name)
		{
			for (int i = 0; i < names.Length; i++) {
				if (names[i] == name)
					return true;
			}

			return false;
		}

		static bool ContainsTag (Tag[] tags, uint id)
		{
			for (int i = 0; i < tags.Length; i++) {
				if (tags[i].Id == id)
					return true;
			}

			return false;
		}
	}
}
