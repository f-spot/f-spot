// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Core.Tests
{
	[TestClass]
	public sealed class TagDeleteTests
	{
		[TestMethod]
		public void HasChildren_FalseForALeafTag ()
		{
			Assert.IsFalse (TagDelete.HasChildren (new Tag (null, 1, "Vacation")));
		}

		[TestMethod]
		public void HasChildren_TrueForACategoryWithAChild ()
		{
			var root = new Category (null, 1, "Root");
			_ = new Tag (root, 2, "Vacation");
			Assert.IsTrue (TagDelete.HasChildren (root));
		}

		[TestMethod]
		public void BlockedCopy_UsesTheGtkEmptyCategoryStrings ()
		{
			var root = new Category (null, 1, "Root");
			Assert.AreEqual (Strings.TagIsNotEmpty, TagDelete.BlockedHeading);
			string body = TagDelete.BlockedMessage (root);
			Assert.Contains ("Root", body);
			Assert.DoesNotContain ("{0}", body);
		}

		[TestMethod]
		public void ConfirmCopy_NamesTheTagAndExplainsPhotoAssociations ()
		{
			var tag = new Tag (null, 1, "Vacation");
			Assert.AreEqual (string.Format (Strings.DeleteTagXQuestion, "Vacation"), TagDelete.ConfirmHeading (tag));
			string none = TagDelete.ConfirmBody (0);
			Assert.AreEqual (Strings.ThisRemovesTheTagFromTheCatalogAndFromAnyPhotosThatUseIt, none);
			string some = TagDelete.ConfirmBody (3);
			Assert.Contains ("3", some);
			Assert.DoesNotContain ("{0}", some);
		}
	}
}
